# ARdventure

A Unity adventure prototype for a removable smart accessory attached to existing toys. Five playable adventures use real camera and microphone discoveries or explicit simulated inputs. No AR, cloud backend or paid assets are required; every adventure also works without a toy.

## Start the adventure demo

Open the project in **Unity 6000.6.7f1**, wait for compilation, then choose **ARdventure → Open Adventure Demo** and press **Play**. The quest board is saved at `Assets/ARdventure/Scenes/QuestBoard.unity`; it is also the first enabled build scene. The existing SampleScene remains registered.

Choose a card with the mouse, press **1–5**, or use **left/right arrows + Enter**. Each adventure opens its own independent screen:

| Adventure | Physical discovery | Without hardware |
| --- | --- | --- |
| Mosswood — The Living Bridge | Find green to learn Vineweave and restore the crossing | G: green, B: blue |
| Prism Hollow — The Sleeping Lanterns | Visit the blue, gold and green lanterns and scan their colours in that order | B: blue, Y: gold, G: green |
| Moonlit Ruins — The Hidden Seal | Shade the lens to reveal a clue, uncover it and scan in light, then touch the rune stones in the remembered order | N: shade, L: light |
| River Crossing — The Steady Crossing | Trackpad/mouse tracing simulates keeping the toy balanced; no IMU is connected | Click and hold the explorer, then trace right inside the glowing bridge strip |
| Echo Grove — The Silver Bell | Walk to the echo stone; one sound on the Sense microphone opens the path automatically | F2: select simulation, O: make a sound |

Use **WASD/arrows** to move, **E/Space** to interact, **T** to scan the real camera, the Sense microphone listens automatically beside the echo stone (**I or L** retries), **Esc** to pause, **R** to restart the current quest, and **M** to return to the quest board. Keep the USB bridge running for real scans; see [hardware setup](firmware/README.md). The selected camera address is retained when switching screens. Completion badges last for the current Play session. Returning to an adventure starts it fresh; there is no saved-progress system.

**River Crossing (4):** click and hold the explorer on the left, then drag right along the gold line. Stay inside the glowing strip. Drifting drains balance; returning to the strip restores it. Sustained drift returns you safely to the bank. Release to rest or reposition your hand, then grab the explorer again to continue. Reaching the right bank completes the quest automatically. This is a trackpad/mouse simulation of steadiness, not accelerometer input.

For Moonlit Ruins, shade the lens gently; a very dark reading reveals the clue. Uncover it and aim at a well-lit surface for the light scan. This is a relative camera-brightness experiment, not a calibrated light meter. The three camera adventures were played through with simulated discoveries; the new blue/gold and shade/light hardware interactions still need testing with physical objects under your lighting.

See [expanded-demo verification and walkthroughs](docs/QUEST-DEMO.md).

## Open and play Mosswood directly

1. Open this project folder (the folder containing `Assets`, `Packages` and `ProjectSettings`) in Unity **6000.6.7f1**.
2. Wait for compilation, then choose **ARdventure → Open Mosswood Prototype**. Alternatively open `Assets/ARdventure/Scenes/Mosswood.unity`.
3. Press **Play**, select the **Game** tab and click inside the game. A landscape view (16:10 or 16:9) works best. The scene builds its art at runtime; an almost empty Scene view before Play is expected.
4. Use **WASD / arrows** to move and **E / Space** to interact. Walk northeast to Pip and talk, then east to inspect the broken crossing.
5. Press **B** to try an incorrect blue scan, then **G** to simulate green and unlock Vineweave. Interact at the crossing again to grow the bridge.
6. Cross along the centre of the vines, walk northeast to the beacon and interact to complete the quest.

**Esc** pauses/resumes; **R** restarts. Buttons also provide scan, contextual interaction, pause and restart. Keyboard movement is required; touch-only mobile play is not implemented.

Stop Play before changing scripts. A script reload during Play can discard the prototype's unsaved runtime quest and object references; stop and start Play again if this happens.

## Verify

Choose **ARdventure → Validate Adventure Demo** outside Play mode to run **151 checks**: 18 bridge-balance checks, 39 motion/microphone checks, 47 camera-adventure checks, the original 31 quest checks and 16 camera-protocol checks. They include ordered lantern scans, clue/rune transitions, wrong/early inputs, restart, simulator events and whole-map reachability on both new maps.

For the bridge's screen/input integration check, enter Play, select River Crossing (4) with a fresh crossing, then choose **ARdventure → Run Bridge Pointer Play Check**. It uses a temporary virtual Mouse inside Unity to check grabbing, tracing, release/resume, slipping and completion. It does not move the operating-system cursor. Keep the Game view focused while it runs. All seven input checks passed, in addition to the 151 model/protocol checks. This complements manual trackpad feel-testing.

Choose **ARdventure → Validate First Quest** outside Play mode. The Console should report **all 31 quest, crossing and simulated-input checks passed**. These check stage ordering, early/wrong scans, real-camera and simulated green acceptance, reset, river restrictions, full-map reachability before/after bridge growth, and simulated-input event delivery/disablement.

For unattended validation with the project closed in the editor:

```sh
"/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath "$PWD" \
  -executeMethod ARdventure.Editor.ForestMilestoneTools.BatchValidate \
  -logFile /tmp/ardventure-validation.log
```

`BatchValidate` exits with 0 on success and 1 on a validation exception, and creates the scene if absent. It does not replace visual play-testing.

## Current scope and context

- [Embedded C++ firmware and real-camera setup](firmware/README.md)
- [Project concept, hardware and implementation principles](Assets/ARdventure/PROJECT-BRIEF.md)
- [Milestone structure, controls and limitations](Assets/ARdventure/README-MILESTONE.md)
- [Local verification record](docs/VERIFICATION.md)
- [Expanded quest demo and verification](docs/QUEST-DEMO.md)

Unity uses URP **17.6.0**, Input System **1.20.1**, and the new Input System (`activeInputHandler: 1`). Preserve existing packages, settings and unrelated content.

`QuestDemo` hosts the quest board and independent adventure screens. `ForestQuest`, `LanternQuest`, `MoonlightQuest`, `BalanceBridgeQuest` and `EchoQuest` hold progression rules. `IToyInput` is the sensor event boundary: `SimulatedToyInput` supplies debug scans and `WifiToyInput` requests physical camera readings over HTTP (via a local USB bridge or directly over Wi-Fi). `IToyMotionInput` and `IToySoundInput` provide separate typed boundaries for motion and sound; `SimulatedMotionInput` and `MicrophoneToyInput` are their adapters. `BalanceBridgeScreen` renders River Crossing; `ActionQuestScreen` renders Echo Grove. The earlier windmill gesture model/adapter remains in source but is no longer launched from the board. `ForestMilestone` renders Mosswood; `DiscoveryQuestScreen` renders the two new maps with shared `AdventureArt` utilities. The XIAO ESP32-S3 Sense has arrived; its camera/microphone/Wi-Fi/BLE are available, while motion, GPS and dedicated colour sensing require additional hardware. See the firmware guide for setup.

The forest, Pip and artwork are placeholders. There are no enemies, save system, narration or object recognition. Camera firmware measures colour; it does not identify objects. Trees are decorative; river and map boundaries constrain movement.

Git already has `origin` configured to `https://github.com/shez-h/ARdventure.git`. Local work has not been committed or pushed by this setup task. Publishing/pushing requires a separate request.
