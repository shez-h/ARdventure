import importlib.util
import json
import os
from pathlib import Path
import pty
import threading
import unittest

spec = importlib.util.spec_from_file_location("toy_usb_bridge", Path(__file__).parents[1] / "toy_usb_bridge.py")
bridge = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bridge)


class SerialBridgeTests(unittest.TestCase):
    def setUp(self):
        self.master, self.slave = pty.openpty()
        self.toy = bridge.ToySerial(os.ttyname(self.slave))

    def tearDown(self):
        self.toy.close()
        os.close(self.master)
        os.close(self.slave)

    def respond(self, body, expected=b"s"):
        def device():
            command = os.read(self.master, 1)
            if command == expected:
                os.write(self.master, b"camera diagnostic\r\n" + body + b"\r\n")
        thread = threading.Thread(target=device, daemon=True)
        thread.start()
        return thread

    def test_forwards_measurement_unchanged(self):
        reading = {"version": 1, "source": "xiao-camera", "sequence": 7, "r": 17, "g": 180, "b": 40, "samples": 19200}
        thread = self.respond(json.dumps(reading).encode())
        status, result = self.toy.capture()
        thread.join(1)
        self.assertEqual(status, 200)
        self.assertEqual(result, reading)

    def test_camera_error_cannot_become_colour(self):
        thread = self.respond(b'{"error":"capture_failed"}')
        status, result = self.toy.capture()
        thread.join(1)
        self.assertEqual(status, 503)
        self.assertEqual(result, {"error": "capture_failed"})

    def test_bad_response_is_rejected(self):
        thread = self.respond(b'{"version":2,"source":"other"}')
        with self.assertRaises(ValueError):
            self.toy.capture()
        thread.join(1)

    def test_binary_snapshot_is_forwarded_exactly(self):
        jpeg = b"\xff\xd8\x01\x02\xff\xd9"
        thread = self.respond(b"JPEG 6\n" + jpeg, expected=b"p")
        self.assertEqual(self.toy.snapshot(), jpeg)
        thread.join(1)

    def test_audio_measurement_unchanged(self):
        reading = {"version": 1, "source": "xiao-microphone", "sequence": 9, "rms": .00825, "peak_rms": .0627, "samples": 32000, "sample_rate": 16000, "duration_ms": 2000, "clipped_fraction": 0}
        thread = self.respond(json.dumps(reading).encode(), expected=b"a")
        self.assertEqual(self.toy.capture(b"a", "xiao-microphone"), (200, reading))
        thread.join(1)

    def test_audio_cannot_use_camera_payload(self):
        thread = self.respond(b'{"version":1,"source":"xiao-camera"}', expected=b"a")
        with self.assertRaises(ValueError):
            self.toy.capture(b"a", "xiao-microphone")
        thread.join(1)

    def test_microphone_error_cannot_become_silence(self):
        thread = self.respond(b'{"error":"microphone_not_ready"}', expected=b"a")
        self.assertEqual(self.toy.capture(b"a", "xiao-microphone"), (503, {"error": "microphone_not_ready"}))
        thread.join(1)


if __name__ == "__main__":
    unittest.main()
