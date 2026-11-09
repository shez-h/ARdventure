# ARdventure

A Unity adventure prototype for a removable smart accessory attached to existing toys. Start with the playable game and simulated toy discoveries; real hardware follows. No AR, cloud backend, paid assets or toy connection is needed for this milestone.

## Open and play Mosswood

1. Open this project folder (the folder containing `Assets`, `Packages` and `ProjectSettings`) in Unity **6000.6.7f1**.
2. Wait for compilation, then choose **ARdventure → Open Mosswood Prototype**. Alternatively open `Assets/ARdventure/Scenes/Mosswood.unity`.
3. Press **Play**, select the **Game** tab and click inside the game. A landscape view (16:10 or 16:9) works best. The scene builds its art at runtime; an almost empty Scene view before Play is expected.
4. Use **WASD / arrows** to move and **E / Space** to interact. Walk northeast to Pip and talk, then east to inspect the broken crossing.
5. Press **B** to try an incorrect blue scan, then **G** to simulate green and unlock Vineweave. Interact at the crossing again to grow the bridge.
6. Cross along the centre of the vines, walk northeast to the beacon and interact to complete the quest.

**Esc** pauses/resumes; **R** restarts. Buttons also provide scan, contextual interaction, pause and restart. Keyboard movement is required; touch-only mobile play is not implemented.

Stop Play before changing scripts. A script reload during Play can discard the prototype's unsaved runtime quest and object references; stop and start Play again if this happens.

## Verify

Choose **ARdventure → Validate First Quest** outside Play mode. The Console should report **all 26 quest, crossing and simulated-input checks passed**. These check stage ordering, early/wrong scans, reset, river restrictions, full-map reachability before/after bridge growth, and simulated-input event delivery/disablement.

For unattended validation with the project closed in the editor:

```sh
"/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath "$PWD" \
  -executeMethod ARdventure.Editor.ForestMilestoneTools.BatchValidate \
  -logFile /tmp/ardventure-validation.log
```

`BatchValidate` exits with 0 on success and 1 on a validation exception, and creates the scene if absent. It does not replace visual play-testing.

## Current scope and context

- [Project concept, hardware and implementation principles](Assets/ARdventure/PROJECT-BRIEF.md)
- [Milestone structure, controls and limitations](Assets/ARdventure/README-MILESTONE.md)
- [Local verification record](docs/VERIFICATION.md)

Unity uses URP **17.6.0**, Input System **1.20.1**, and the new Input System (`activeInputHandler: 1`). Preserve existing packages, settings and unrelated content.

`ForestQuest` holds progression and map rules. `IToyInput` is the sensor event boundary, with `SimulatedToyInput` as the current adapter. `ForestMilestone` builds the world, handles conventional controls and displays the HUD. Keep future local Wi-Fi transport separate from gameplay. The ordered XIAO ESP32-S3 Sense has camera/microphone/Wi-Fi/BLE, but motion, GPS and dedicated colour sensing require additional hardware. See the brief for limitations.

The forest, Pip and artwork are placeholders. There are no enemies, save system, narration, hardware recognition or real toy transport. Trees are decorative; river and map boundaries constrain movement.

Git already has `origin` configured to `https://github.com/shez-h/ARdventure.git`. Local work has not been committed or pushed by this setup task. Publishing/pushing requires a separate request.
