# ARdventure: Mosswood — first playable milestone

**Hardware extension:** embedded C++ camera firmware, a USB bridge and an optional direct Wi-Fi input path are now included in the repository. See `firmware/README.md` at the project root. The original milestone description below records the simulator-only baseline; G/B remain simulated, while T/Scan toy requests a real camera reading.

This is a Unity 6 / C# top-down adventure for the connected-toy project. It needs the Unity Input System package (already installed in the destination project). No paid assets, server, toy hardware or additional Unity packages are required.

## Install into the existing Unity project

Copy **only the `Assets/ARdventure` folder** from this package into your project's `Assets` folder. Do not replace your Packages, ProjectSettings or existing scenes. Also copy this README and PROJECT-BRIEF.md into the project root if desired.

Let Unity compile. Choose **ARdventure → Open Mosswood Prototype** from the top menu, then press **Play**. The menu creates and saves the scene if it does not already exist. The scene creates its art and game objects at runtime, so the editor scene is mostly empty until Play begins. Open the Game tab and use a landscape aspect ratio, ideally 16:10 or 16:9.

## Play the quest

- WASD or arrow keys: move. Click the Game view if keys do not respond.
- E or Space: talk, inspect or use the ability when close to the relevant object. The on-screen interaction button also works.
- G / Scan green: emit a simulated green scan.
- B / Scan blue: emit an incorrect scan, to check rejection behaviour.
- Esc / Pause: pause or resume.
- R / Restart quest: return to the beginning.

Walk northeast to Pip, speak to them, then follow the path east to the river and inspect the crossing. Scan green, interact with the crossing again to grow the bridge, walk across its centre, and interact with the woodland beacon.

Early scans do not skip the introduction; blue scans do not solve the puzzle; the river stays blocked until the player activates the earned ability. Other river routes stay blocked after the bridge grows. Restart clears the entire quest.

## Files and responsibilities

- `ForestQuest.cs`: quest state, valid transitions, colour acceptance, and river crossing rules.
- `SimulatedToyInput.cs`: `IToyInput` event interface and debug input adapter.
- `ForestMilestone.cs`: runtime scene construction, keyboard player, interactions, bridge animation, HUD and feedback.
- `Editor/ForestMilestoneTools.cs`: menu to open the prototype and deterministic progression/input/boundary checks.

Choose **ARdventure → Validate First Quest** to run the checks in the editor; the Console reports success or a specific failure.

## Deliberate prototype limits

This is a single quest using procedural placeholder art and immediate-mode UI. It has no enemies, save system, real sensor transport, narration or hardware image recognition. Trees are decorative; the map edge and river enforce movement boundaries. The complete map is visible, so there is no camera-follow system. It is designed for keyboard testing on a Mac, not yet for touch-only mobile play.

The camera is built in the runtime scene, not taken from the physical toy. The G/B controls simulate colour readings. No real Wi-Fi/BLE connection is claimed.

The future toy adapter can implement `IToyInput` and replace the simulator subscription. The quest model does not depend on the transport. Android/iOS permissions and networking belong in that adapter, not in puzzle code.

## Next milestone

Play-test the loop, then connect one real green colour reading over local Wi-Fi. Keep debug inputs available for developing without hardware.
