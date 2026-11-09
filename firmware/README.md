# Embedded C++ camera toy

This firmware targets the **Seeed Studio XIAO ESP32-S3 Sense** with its camera expansion board and antenna. No additional sensors are needed. It captures a real camera image, computes the average colour of its central region on the ESP32, and returns the result to Unity over USB or local Wi-Fi.

## First upload

Connect the expansion board and supplied antenna with USB power disconnected, then connect the XIAO using a USB **data** cable. Leave it USB powered for the first demo.

Use [Arduino CLI](https://docs.arduino.cc/arduino-cli/) or the Arduino IDE with Espressif's official board index:

`https://espressif.github.io/arduino-esp32/package_esp32_index.json`

The project pins **esp32:esp32 3.3.12**. Select **XIAO_ESP32S3**, **OPI PSRAM**, **USB CDC On Boot: Enabled**. Open `ardventure_toy/ardventure_toy.ino` in the IDE, or use:

```sh
arduino-cli core update-index --additional-urls https://espressif.github.io/arduino-esp32/package_esp32_index.json
arduino-cli core install esp32:esp32@3.3.12 --additional-urls https://espressif.github.io/arduino-esp32/package_esp32_index.json
arduino-cli board list
arduino-cli compile --fqbn esp32:esp32:XIAO_ESP32S3:PSRAM=opi firmware/ardventure_toy
arduino-cli upload --fqbn esp32:esp32:XIAO_ESP32S3:PSRAM=opi --port /dev/cu.usbmodem1101 firmware/ardventure_toy
```

Run commands from the repository root; substitute the current USB port from `board list`. Upload replaces the board's current sketch. If upload cannot enter download mode, hold BOOT, tap RESET, release BOOT, and check the port again. After upload, tap RESET if needed. Serial output is at **115200 baud**.

## Play with real hardware over USB (first demo)

Keep the board plugged in and close any serial monitor, then run from the repository root:

```sh
python3 tools/toy_usb_bridge.py --port /dev/cu.usbmodem1101
```

This bridge uses Python's standard library, runs only on loopback, and forwards each capture command to the real board. It does not calculate or simulate colours. In Unity, enable **Toy camera** and leave the address at **http://127.0.0.1:8765**. Press **T / Scan toy**. All camera capture and image processing still run in embedded C++. The Mac keeps its normal Wi-Fi connection. Stop the bridge with Ctrl-C before uploading new firmware. If the USB port changes, restart with the new port.

**T** also enables toy-camera input automatically, so the entire demo can be operated from the keyboard. If aiming is difficult, open [a fresh camera snapshot](http://127.0.0.1:8765/v1/frame) while the USB bridge is running. Refresh to capture another image. This diagnostic transfers the JPEG only to the local Mac; ordinary scans transfer RGB numbers.

## Optional direct Wi-Fi demo

1. Join **ARdventure-Toy** from the Mac's Wi-Fi menu. Password: **mosswood-demo**. This demo access point has no internet connection; reconnect to your normal Wi-Fi afterwards.
2. Open Mosswood in Unity, press Play, and enable **Toy camera** at the lower left. Change the address to **http://192.168.4.1**, then click **Scan toy** to leave the text field.
3. Aim the camera at a brightly lit, plain coloured object that fills the central portion of the view. Start with coloured paper; avoid screens, shadows and reflections when establishing a baseline.
4. Press **T** or **Scan toy**. The HUD shows measured RGB channels. A scan before the discovery mission must not advance the quest.
5. Talk to Pip and inspect the crossing. Scan a blue object to see rejection, then a green object to unlock Vineweave. Activate the bridge separately and complete the quest.

**G/B remain explicitly simulated inputs**. For a hardware demonstration, use T/Scan toy and show the RGB reading changing with physical objects. Scan controls are disabled during pause/completion, and in-flight responses are cancelled on restart, pause, disabling hardware input or changing address.

The first capture becomes available after a two-second exposure warm-up. A camera error produces a retryable message instead of a fake colour. Disconnecting the board must leave keyboard play and simulator inputs working.

## Protocol

The board runs a single-client demo access point, default address `192.168.4.1`. HTTP is local, with no cloud dependencies, SD writes or home-network credentials. Ordinary scans return numbers; the USB bridge's optional `/v1/frame` diagnostic transfers a JPEG locally for aiming. The fixed password and unauthenticated HTTP endpoints are for a local prototype, not production access control.

- `GET /v1/status`: version, source and camera-ready flag.
- `POST /v1/scan`: acquire a fresh JPEG frame, decode into a reusable PSRAM buffer, sample its centre, return a measured reading.
- Capture/not-ready failures: HTTP 503; decode/sampling failures: HTTP 500. No successful colour event is emitted on failure.

Example successful body:

```json
{"version":1,"source":"xiao-camera","sequence":1,"r":30,"g":200,"b":40,"samples":19200,"capture_ms":85}
```

RGB is 0–255. Unity validates version, source, positive sequence/sample count and channel bounds, then normalises to its `Color` type. Each request is a deliberate capture; scans are never cached/replayed and are not automatically retried. The sequence is a diagnostic counter since boot, not a persistent event identifier.

The quest accepts green hues from **75° to 165°**, with saturation at least **25%** and brightness at least **25%**. These prototype limits include muted camera greens while excluding cyan, yellow, neutral and very dark readings. A recorded physical green sample (55, 86, 70) is covered by the editor checks. Lighting and backgrounds still matter; this is a first calibration, not a broad colour-recognition benchmark.

## What the C++ demonstrates

- Embedded camera configuration, PSRAM allocation and error reporting.
- RAII ownership: every camera frame returns to the driver, including error paths; the reusable image buffer has explicit ownership.
- Bounded image processing using integer accumulation, independent of Arduino and Unity.
- A small versioned HTTP interface separating measurement from quest rules.
- A cooperative main loop, optional serial diagnostics and finite USB-serial startup wait.

The camera captures at 320×240 JPEG. Espressif's `fmt2rgb888` decoder stores channels in BGR order; `ColourSampler.h` makes that boundary explicit. The centre half of each image axis gives 19,200 sampled pixels. This is colour measurement, not object recognition or a calibrated colour sensor. Auto white balance and illumination affect results. Fixed-focus and autofocus camera variants need physical testing; detailed autofocus is outside this first colour prototype.

## Native C++ checks

On the Mac, without board libraries:

```sh
clang++ -std=c++17 -Wall -Wextra -Werror -fsanitize=address,undefined \
  firmware/tests/colour_sampler_tests.cpp -o /tmp/ardventure-colour-tests
/tmp/ardventure-colour-tests
```

Checks exercise buffer bounds, empty/oversize images, channel order, centre-region isolation, black/white inputs and full-size accumulation. Unity's **ARdventure → Validate Wi-Fi Toy Protocol** checks valid/malformed payloads and quest gating; **Validate First Quest** remains available.

Run the USB bridge's pseudo-terminal tests with `python3 -B -m unittest discover -s tools/tests -v`.

## References

- [Seeed camera setup and pinout](https://wiki.seeedstudio.com/xiao_esp32s3_camera_usage/)
- [Seeed board setup, camera variants and PSRAM](https://wiki.seeedstudio.com/xiao_esp32s3_getting_started/)
- [Espressif camera driver](https://github.com/espressif/esp32-camera)
- [Espressif Arduino core](https://github.com/espressif/arduino-esp32)

For this development session Arduino CLI and board tools were placed under `/tmp/ardventure-arduino`, with an isolated `arduino-cli.yaml`. That directory is temporary; the source and these reproducible setup instructions are the durable repository artifacts.

On this Apple Silicon Mac the bundled Arduino ctags helper was Intel-only and could not run. A native helper was built from the official `arduino/ctags` tag `5.8-arduino11` (commit `abc8fca7499f44c725122881cd380a88c37abe0e`) under `/tmp/ardventure-ctags`, using `./configure` and `make`. Its reserved `__unused__` macro was renamed to `CTAGS_UNUSED` in its own C/header sources to avoid a collision with the current macOS SDK. The successful local compile command adds `--build-property runtime.tools.ctags.path=/tmp/ardventure-ctags` and uses `--config-file /tmp/ardventure-arduino/arduino-cli.yaml`. No Rosetta or system-wide helper replacement was needed.
