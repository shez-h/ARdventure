# ARdventure project context

We are exploring a removable smart accessory for existing toys/action figures. It lets a child bring physical discoveries into a digital action/adventure. The toy remains independently playable. The game itself needs to be enjoyable beyond the novelty of connected hardware.

Target audience: approximately 5–12; younger children may have adult help. Hints and narration are future considerations. Prototype gameplay should first be tested at a narrower difficulty level.

The user is an experienced C# engineer learning Unity. Unity 6000.6.7f1 and the new Input System 1.20.1 were present when this milestone was prepared. Initial development is on Mac; phone/tablet support may follow. Console release is not prototype scope.

Ordered: XIAO ESP32-S3 Sense (processor, camera, microphone, Wi-Fi/BLE and microSD slot). Motion, GPS and dedicated colour sensing require separate modules. Budget options discussed: MPU-6050/GY-521, TCS34725, NEO-6M. Their purchase is not confirmed. Other candidate inputs include a button or capacitive touch pad.

Physical missions may involve colour, photographs, carrying/movement, light or optional outdoor destinations. A carried toy cannot be assumed to count steps accurately. GPS is for outdoor, metre-scale location, not room tracking. AR is not needed for the first game.

First proposed experiment: explore Mosswood, meet Pip, inspect a broken crossing, discover green, activate a growing ability, cross the vine bridge, and light the beacon. Forest theme and temporary characters are not final branding.

Keep gameplay independent of hardware input. Start with explicit simulated events and keyboard controls; integrate local Wi-Fi later. Avoid cloud dependencies, accounts, paid assets, unnecessary frameworks and premature architecture. Preserve existing project content and use compatible APIs.
