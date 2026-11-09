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

## Microphone expansion — 9 November 2026

The camera + microphone firmware compiled and was uploaded to the same Sense board: **1,002,382 bytes flash**, **61,232 bytes static RAM**. The existing PSRAM image buffer remains separate. `ESP_I2S` PDM receive at 16 kHz mono ran alongside the camera.

- Native `SoundMeter.h` checks passed with AddressSanitizer and UndefinedBehaviorSanitizer for silence, DC rejection, known RMS, transient peak retention, clipping and sample-count bounds. Existing colour-sampler sanitizer checks also passed.
- All **seven** Python pseudo-terminal tests passed, including exact microphone-statistics forwarding, sensor-source mismatch rejection, and microphone errors remaining errors rather than becoming silence.
- **135** Unity checks passed: 41 new motion/microphone checks plus the existing 94 adventure/protocol checks.
- A real two-second USB microphone request returned RMS **0.0082502**, peak-window RMS **0.0627389**, clipping **0**, **32,000 samples** at **16 kHz**. A subsequent camera request returned RGB **78,82,73**, **19,200 samples**, **80 ms**, verifying both sensors after the firmware update.
- Echo Grove requested a real listen through Unity and accepted a quiet calibration reading (HUD rounded RMS **0.0002**, peak **0.0003**). It advanced to the call stage. A later real microphone reading (RMS **0.0072668**, peak **0.0637199**) advanced the running Unity quest to **Let the echo settle**. The final quiet/collection step has not yet been observed.
- Windmill Ridge was played through with keyboard movement and simulated shake/left/right/steady gestures. Observed the final objective and completion overlay after collecting the feather.

Microphone values are normalised digital amplitudes, not calibrated SPL. No raw audio is sent to Unity or written to disk. Motion is explicitly simulated because this board has no IMU. Direct Wi-Fi microphone input and standalone builds remain untested.

## Simplified Echo Grove revision

User feedback identified the multi-step sound puzzle and I/l ambiguity as confusing. Echo Grove now listens automatically beside the stone, opens the bridge after one valid sound, and accepts both I (capital i) and L for manual retries. Journal reading, calibration and quiet requirements were removed. Connection details are optional UI. **133 updated Unity checks passed** (39 action/microphone, 47 camera-adventure, 31 Mosswood, 16 camera protocol). The complete simplified screen was played through using one simulated O event, bridge crossing and bell collection. Existing microphone firmware and USB protocol are unchanged.

Verified the revised Unity screen automatically requests `/v1/listen` upon approaching the stone without a key or journal interaction. The connected board returned consecutive quiet readings (for example RMS 0.0002791, peak 0.0010434); these correctly left the path closed. Left the player at the stone with real automatic listening active for the user's clap/voice test. The new live sound-to-open transition has not yet been physically observed; the recorded real sound and full simulated completion are covered as described above.
