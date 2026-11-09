#!/usr/bin/env python3
"""Mac/Linux USB serial -> loopback HTTP bridge; all image processing runs on the toy."""
import argparse
import json
import os
import select
import termios
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer


class ToySerial:
    def __init__(self, port):
        self.fd = os.open(port, os.O_RDWR | os.O_NOCTTY | os.O_NONBLOCK)
        try:
            settings = termios.tcgetattr(self.fd)
            settings[0] = 0
            settings[1] = 0
            settings[2] = termios.CS8 | termios.CREAD | termios.CLOCAL
            settings[3] = 0
            settings[4] = settings[5] = termios.B115200
            settings[6][termios.VMIN] = settings[6][termios.VTIME] = 0
            termios.tcsetattr(self.fd, termios.TCSANOW, settings)
        except Exception:
            os.close(self.fd)
            raise
        self.lock = threading.Lock()

    def capture(self, command=b"s", source="xiao-camera"):
        with self.lock:
            termios.tcflush(self.fd, termios.TCIFLUSH)
            os.write(self.fd, command)
            deadline = time.monotonic() + 4
            buffer = b""
            while time.monotonic() < deadline:
                readable, _, _ = select.select([self.fd], [], [], max(0, deadline - time.monotonic()))
                if not readable:
                    break
                chunk = os.read(self.fd, 512)
                if not chunk:
                    raise OSError("Toy disconnected")
                buffer += chunk
                if len(buffer) > 4096:
                    raise ValueError("Oversized serial response")
                while b"\n" in buffer:
                    line, buffer = buffer.split(b"\n", 1)
                    if not line.startswith(b"{"):
                        continue  # Skip serial diagnostics, never generate synthetic sensor data.
                    reading = json.loads(line)
                    if not isinstance(reading, dict):
                        raise ValueError("Invalid serial response")
                    if "error" in reading:
                        return 503, reading
                    if reading.get("version") != 1 or reading.get("source") != source:
                        raise ValueError("Unexpected toy protocol")
                    return 200, reading
            raise TimeoutError("No sensor response within four seconds")

    def close(self):
        os.close(self.fd)

    def snapshot(self):
        with self.lock:
            termios.tcflush(self.fd, termios.TCIFLUSH)
            os.write(self.fd, b"p")
            deadline = time.monotonic() + 4
            buffer = b""
            length = None
            while time.monotonic() < deadline:
                ready, _, _ = select.select([self.fd], [], [], max(0, deadline - time.monotonic()))
                if not ready:
                    break
                chunk = os.read(self.fd, 4096)
                if not chunk:
                    raise OSError("Toy disconnected")
                buffer += chunk
                if length is None:
                    while b"\n" in buffer:
                        line, buffer = buffer.split(b"\n", 1)
                        if line.startswith(b"{"):
                            raise ValueError("Camera snapshot failed")
                        if line.startswith(b"JPEG "):
                            length = int(line[5:])
                            if length < 2 or length > 131072:
                                raise ValueError("Invalid JPEG length")
                            break
                    if length is None and len(buffer) > 4096:
                        raise ValueError("Oversized snapshot header")
                if length is not None and len(buffer) >= length:
                    frame = buffer[:length]
                    if not frame.startswith(b"\xff\xd8"):
                        raise ValueError("Invalid JPEG frame")
                    return frame
            raise TimeoutError("No camera snapshot within four seconds")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", required=True, help="USB port from arduino-cli board list")
    parser.add_argument("--http-port", type=int, default=8765)
    args = parser.parse_args()
    toy = ToySerial(args.port)

    class Handler(BaseHTTPRequestHandler):
        def do_GET(self):
            if self.path != "/v1/frame" or self.headers.get("Origin"):
                self.send_error(404)
                return
            try:
                frame = toy.snapshot()
            except (OSError, ValueError, TimeoutError) as error:
                self.send_error(503, str(error))
                return
            self.send_response(200)
            self.send_header("Content-Type", "image/jpeg")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(frame)))
            self.end_headers()
            self.wfile.write(frame)

        def do_POST(self):
            # Empty deliberate scan requests only; reject browser cross-origin requests.
            if self.path not in ("/v1/scan", "/v1/listen") or self.headers.get("Origin"):
                self.send_error(404)
                return
            try:
                status, body = toy.capture(b"a", "xiao-microphone") if self.path == "/v1/listen" else toy.capture()
            except (OSError, ValueError, TimeoutError) as error:
                print(f"Capture failed: {error}", flush=True)
                status, body = 503, {"error": "usb_capture_failed"}
            encoded = json.dumps(body).encode("utf-8")
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(encoded)))
            self.end_headers()
            self.wfile.write(encoded)
            print(json.dumps(body), flush=True)

    try:
        server = ThreadingHTTPServer(("127.0.0.1", args.http_port), Handler)
        print(f"Real USB toy {args.port} -> http://127.0.0.1:{args.http_port}; Ctrl-C to stop", flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass
        finally:
            server.server_close()
    finally:
        toy.close()


if __name__ == "__main__":
    main()
