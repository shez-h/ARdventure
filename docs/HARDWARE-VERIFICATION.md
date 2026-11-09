# Camera hardware integration — 9 November 2026

## Hardware and build

The user's XIAO ESP32-S3 Sense is connected at `/dev/cu.usbmodem1101`, with no additional sensors. Espressif's uploader identified an ESP32-S3 revision 0.2 with 8 MB embedded PSRAM. Built with Arduino CLI 1.5.1 and official `esp32:esp32` core 3.3.12, targeting `XIAO_ESP32S3:PSRAM=opi`.

Final preview-enabled firmware compiled successfully: **970,554 bytes flash**, **61,104 bytes static RAM**. The 230,400-byte decoded-image buffer is allocated separately in PSRAM at runtime. Uploaded and verified the firmware successfully using esptool 5.3.1.

The Intel-only Arduino ctags helper failed on this Mac. Built a native helper from Arduino's official source and supplied its path to the compiler. See `firmware/README.md` for the exact version and workaround. The rest of the toolchain ran normally.

## Checks completed

- Native C++ colour-sampler checks passed with AddressSanitizer and UndefinedBehaviorSanitizer: buffer bounds, invalid dimensions, channel order, centre sampling and full-frame white/black data.
- Four Python serial-bridge checks passed against a pseudo-terminal, including exact measured-payload forwarding, error/invalid-protocol rejection and binary snapshot forwarding. These are transport tests, not claims of camera hardware validation.
- Unity's 16 protocol checks passed; the existing 26 quest checks passed after initial integration.
- Real board camera acquisition, JPEG decoding and colour sampling worked over USB. First measured payload: RGB **136, 143, 142**, **19,200 samples**, **86 ms** capture/processing.
- Unity requested and received a real board scan through the USB bridge. An early scan left the quest at the introduction, as required. A subsequent real reading was RGB **161, 165, 164**, with **69 ms** capture/processing.
- Retrieved and visually inspected a real **320×240 JPEG** using the USB snapshot diagnostic. The camera image was clear and showed a neutral ceiling/background, providing a way to diagnose aiming rather than changing quest thresholds.
- A physical green object produced RGB **55, 86, 70**, **19,200 samples**, **72 ms**. Unity displayed that actual reading. It narrowly missed the original synthetic-colour ratio threshold; acceptance was updated to hue **75–165°**, saturation **≥25%** and brightness **≥25%**. The recorded muted green, original simulated green, cyan/yellow/neutral/dark rejection, stage ordering and river checks all passed in the expanded **31-check** Unity validator.
- After calibration, the user aimed at the green object and pressed **T** in Unity. They confirmed that the objective changed to **Return to the crossing and grow a vine bridge**. This establishes the real sensor → embedded C++ → USB bridge → Unity → quest-unlock loop. The game was left at the unlocked crossing for continued play; the USB bridge remained running.

## Current limits

Direct Wi-Fi firmware compiles, but the Mac has not joined the board's access point and that path has not been tested end to end. The real USB path is the initial demonstration route. Colour is lighting-dependent camera measurement, not object recognition or calibrated sensing. Mobile/standalone builds and physical-button input remain outside this integration.

The USB bridge runs only on `127.0.0.1:8765`; ordinary scans forward RGB numbers. The optional `/v1/frame` endpoint transfers one camera JPEG locally for aiming. It is not a continuous recording or cloud stream.
