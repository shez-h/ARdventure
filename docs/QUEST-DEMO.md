# Expanded adventure demo — 9 November 2026

## Entry and navigation

Choose **ARdventure → Open Adventure Demo**, then Play. `QuestBoard.unity` hosts a selection screen and five independent runtime adventure screens. No external scene packages or asset dependencies were added. The original `Mosswood.unity` scene and its editor command remain available for focused testing.

The board supports mouse selection, 1–5 shortcuts and arrows + Enter. **M** returns to the board during any adventure, including pause and completion. **R** resets only the current quest. Every selection creates a fresh quest model, player, scenery, input adapters and camera. Returning disables the old root before destruction, stops its simulator and aborts outstanding HTTP requests. The shared toy address persists for the session. Completed badges are session-only and survive replaying an adventure within that session; exiting Play clears them.

The editor command places QuestBoard first in the build-scene list and preserves the other registered scenes. This work has not produced or tested a standalone player build.

## Adventures

### Mosswood — The Living Bridge

Original loop preserved: meet Pip, inspect the broken crossing, discover green, activate Vineweave at the crossing, cross and light the beacon. Keyboard controls and real camera input are retained. G/B remain green/blue simulator controls.

### Prism Hollow — The Sleeping Lanterns

1. Walk up to the entrance tablet and press E.
2. Visit the **blue** lantern on the upper-left branch; scan blue (T with a blue object, or B).
3. Visit the **gold** lantern on the lower-middle branch; scan gold/yellow (T or Y).
4. Visit the **green** lantern on the upper-right branch; scan green (T or G).
5. Return to the centre path. The shadow gate now opens; walk through and interact with the guiding star.

Scanning elsewhere or out of order does not bank colours or advance the quest. Wrong colours leave the current lantern waiting. All three discoveries are needed before the gate can be crossed. Blue accepts hue 190–260°, saturation/brightness ≥25%; gold accepts hue 35–75°, saturation/brightness ≥35%. Green retains the measured-camera tolerance used by Mosswood.

### Moonlit Ruins — The Hidden Seal

1. Walk up to the moon seal by the entrance and inspect it with E.
2. Shade the camera and scan with T, or simulate shade with N. A clue appears: **STAR > MOON > LEAF**.
3. Remember the order. Uncover the lens and scan a well-lit surface (T), or simulate light with L. The clue disappears and the stones become active.
4. Touch the **STAR** stone on the upper-right branch, then **MOON** on the upper-left branch, then **LEAF** on the lower branch, using E at each.
5. The arch opens. Follow the centre path through it and collect the moonstone with E.

A wrong rune resets the partial sequence without restarting the adventure. Shade and scan again for a reminder, then scan light to resume. Early scans and rune touches cannot skip the seal or light-discovery stages. Shade requires the brightest RGB channel ≤18%; light requires a channel ≥45%. These are prototype brightness thresholds affected by camera exposure, not lux measurements. No motion sensor, microphone, recognition service or firmware update is required.

### Windmill Ridge — The Sky Feather

1. Read the windkeeper's journal at the entrance (E).
2. Visit the windmill on the centre path and shake its gears (X).
3. Visit the north vane and tilt left (Z), then the south vane and tilt right (C).
4. Return to the windmill and hold steady (V). Cross the restored bridge and collect the sky feather (E).

All gestures are **explicitly simulated**, through `IToyMotionInput`. The Sense board has no IMU. Wrong gestures, early gestures and gestures sent away from the current station do not advance the puzzle. A future sensor adapter can emit the same typed events without changing progression rules.

### Echo Grove — The Silver Bell

1. Walk right to the glowing **ECHO STONE**. Reading the journal is optional.
2. Clap or speak near the Sense board. It listens automatically in repeated two-second windows while you stand beside the stone. **One detected sound opens the bridge immediately.**
3. Walk right across the bridge and collect the silver bell with **E**.

There is no calibration or final quiet step. The prompt changes from “walk to the stone” to “clap or speak” to “path open”. A detected peak-window RMS of at least 0.01 counts as sound; it is a prototype digital threshold, not a sound-pressure measurement. The bridge remains open after the first sound. Pausing, walking away or leaving the screen cancels listening. It stops after the bridge opens; sound away from the stone cannot advance the quest.

Real input uses the **Sense microphone**, with the existing updated firmware and running USB bridge. **I** (capital i) and **L** both work as manual retries, and there is an on-screen button. No key is required for automatic listening. Errors show a connection message and remain retryable; they never count as sound. Connection settings and measurements are available in the optional settings area rather than the main gameplay prompt.

For hardware-free play, press **F2** to select simulated input, walk beside the stone and press **O** once. Source switching preserves an already opened path; R restarts it. Only numerical statistics leave the board; this detects sound levels, not words.

## Implementation

- `QuestDemo.cs`: quest board, screen lifetime and session completion badges.
- `DiscoveryQuests.cs`: transport-independent lantern and moon quest state, shared colour predicates and new map boundaries.
- `DiscoveryQuestScreen.cs`: new world construction, movement, contextual actions, scan handling and HUD.
- `AdventureArt.cs`: generated sprite/camera helpers and shared immediate-mode GUI styles.
- `SimulatedToyInput.cs`: extended with gold, dark and light events; keyboard scanning is suspended while editing the toy address.
- `WifiToyInput.cs`: retains the chosen address across screens; request cancellation remains owned by the adapter.
- `ActionQuests.cs`: independent motion and one-sound quest state machines, typed input events and sound-reading validation.
- `ActionQuestScreen.cs`: the two non-colour worlds, gesture stations, microphone controls and explicit simulation mode.
- `SimulatedMotionInput.cs` / `MicrophoneToyInput.cs`: replaceable gesture simulator and cancellable microphone HTTP transport.
- `QuestDemoTools.cs` / `ActionQuestChecks.cs`: scene creation/registration and expanded validation command.

The new worlds are independent screens inside the entry scene, rather than separate serialized Unity scenes. This keeps switching explicit and avoids scene-name/build-index dependencies while satisfying the separate-screen demo flow. Maps and artwork remain deliberately small procedural placeholders.

## Verification

Unity 6000.6.7f1 compiled the new scripts. **Validate Adventure Demo** passed **133 checks**: 39 motion/microphone checks, 47 camera-adventure checks, 31 existing Mosswood checks and 16 protocol checks. New checks exercise early/wrong scans, ordering, location-node IDs, repeated inputs, rune reset/reminder flows, all reset states, extended simulated events and full-map reachability before/after both gates open.

Visually played through all three adventures in Unity's landscape Game view using keyboard movement and simulated sensor inputs. Confirmed Prism Hollow's ordered blue/gold/green completion, Moonlit Ruins' clue reveal/hide and wrong-rune reminder flow, rune completion and moonstone collection, and Mosswood's complete original loop when launched from the new board. Navigated between independent screens using M and 1/2/3, and exercised arrow + Enter selection. Observed each completion overlay and final objective. A world-label overlap found during this run was corrected and visually rechecked. Verified that shade/light simulator keys do not advance Moonlit Ruins while paused, and exercised restart and return-to-board after the final changes. Left the running demo on the quest board.

New real-camera blue/gold and shade/light missions have not yet been physically play-tested. The camera protocol is unchanged; real green scans were previously verified in Mosswood. Camera colour and exposure calibration may need adjustment under different lighting. Mobile touch-only play, narration, save files and standalone builds remain untested or unimplemented.

No commits or pushes were performed. The working tree was clean before this expansion.

### Non-colour quest verification

Windmill Ridge was played through to its completion overlay using simulated gestures at the required stations. After user feedback, Echo Grove was simplified to a single detected sound, with no journal prerequisite, calibration or quiet stage. The updated model/protocol checks pass, including sound away from the stone, silence rejection, a recorded real-board sound opening the path immediately, repeated readings preserving the open path, threshold boundaries and restart. The simplified screen was played through to the completion overlay using F2 and one O event at the stone, crossing and collecting the bell.

The microphone-enabled firmware was compiled, uploaded and verified on the connected board. Native colour and sound tests passed with address/undefined-behaviour sanitizers, and all seven bridge pseudo-terminal tests passed. See `HARDWARE-VERIFICATION.md` for actual readings and `firmware/README.md` for reproducible setup. The updated USB bridge remains running. Stop Play before editing runtime-generated screen scripts.

Automatic microphone acquisition was verified in the revised Unity screen with real board readings while standing at the stone. Quiet readings correctly did not open the path. The player was left beside the stone with real listening active, ready for a clap or voice. The simplified live sound-to-open transition awaits physical confirmation.
