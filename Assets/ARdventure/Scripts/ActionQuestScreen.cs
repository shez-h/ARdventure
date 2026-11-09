using UnityEngine;
using UnityEngine.InputSystem;

namespace ARdventure
{
    public sealed class ActionQuestScreen : MonoBehaviour
    {
        [HideInInspector] public bool Echo;
        private readonly WindmillQuest wind = new WindmillQuest();
        private readonly EchoQuest echo = new EchoQuest();
        private AdventureArt art;
        private AdventureGui gui;
        private MicrophoneToyInput microphone;
        private SimulatedMotionInput motionInput;
        private Transform hero, sails;
        private SpriteRenderer gate, stone;
        private bool paused, simulatedSound, showConnection;
        private float nextListen;
        private string message, lastSound = "No sound reading yet";
        private float pulse;
        public bool EditingAddress { get; private set; }
        public bool Complete => Echo ? echo.Stage == EchoStage.Complete : wind.Stage == WindmillStage.Complete;
        private bool GateOpen => Echo ? echo.GateOpen : wind.GateOpen;
        private string Objective => !Echo ? wind.Objective : echo.GateOpen ? echo.Objective
            : Near(WindmillQuest.Mill) ? (simulatedSound ? "Press O to make a sound and open the path" : "Listening at the echo stone — clap or speak near the toy!")
            : "Walk right to the glowing ECHO STONE. One sound opens the path.";
        private Color Accent => AdventureArt.Hex(Echo ? "A0D7DA" : "E7C38A");
        private bool Playing => !paused && !Complete;
        private void Start()
        {
            art = new AdventureArt(transform, AdventureArt.Hex(Echo ? "18383D" : "354951"));
            microphone = gameObject.AddComponent<MicrophoneToyInput>();
            microphone.Sound += RealSound;
            motionInput = gameObject.AddComponent<SimulatedMotionInput>();
            motionInput.Enabled = !Echo; motionInput.Motion += Motion;
            BuildWorld(); Restart();
        }
        private void OnDisable() { if (microphone != null) microphone.CancelPending(); if (motionInput != null) motionInput.Enabled = false; }
        private void OnDestroy() { if (microphone != null) microphone.Sound -= RealSound; if (motionInput != null) motionInput.Motion -= Motion; art?.Dispose(); }
        private void Restart()
        {
            microphone.CancelPending(); wind.Reset(); echo.Reset(); paused = false;
            hero.position = DiscoveryMaps.Spawn; nextListen = 0; lastSound = "No sound reading yet";
            message = Echo ? "Walk to the glowing echo stone, then clap or speak near the toy. It listens automatically and opens the bridge after one sound."
                : "The windmill has stopped. Read the journal to learn how to free it. Gestures are simulated: the Sense board has no motion sensor.";
        }
        private void Pause() { paused = !paused; if (paused) microphone.CancelPending(); }
        private void Update()
        {
            motionInput.Enabled = !Echo && Playing; motionInput.KeyboardEnabled = !EditingAddress;
            var k = EditingAddress ? null : Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) Pause();
            if (k != null && k.rKey.wasPressedThisFrame) Restart();
            if (Echo && k != null && k.f2Key.wasPressedThisFrame) SetSoundMode(!simulatedSound);
            if (Playing && k != null)
            {
                var direction = new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
                Move(direction.normalized * (3.5f * Time.deltaTime));
                if (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Interact();
                if (Echo)
                {
                    if (k.iKey.wasPressedThisFrame || k.lKey.wasPressedThisFrame) Listen();
                    if (simulatedSound && k.oKey.wasPressedThisFrame) Simulate(ToySoundReading.Call);
                }

            }
            if (Echo)
            {
                bool canListen = Playing && !GateOpen && !simulatedSound && !EditingAddress && Near(WindmillQuest.Mill);
                if (!canListen) microphone.CancelPending();
                else if (!microphone.Busy && Time.unscaledTime >= nextListen) Listen();
            }
            if (GateOpen) sails.Rotate(0, 0, -65 * Time.deltaTime);
            pulse = Mathf.MoveTowards(pulse, 0, Time.deltaTime);
            stone.color = Color.Lerp(AdventureArt.Hex("6C9698"), Accent, Echo && !GateOpen ? .65f + .35f * Mathf.Sin(Time.unscaledTime * 3) : pulse);
            gate.color = GateOpen ? new Color(Accent.r, Accent.g, Accent.b, .15f) : AdventureArt.Hex("8A8576");
            art.FitCamera();
        }
        private bool Near(Vector2 point) => Vector2.Distance(hero.position, point) < 1.5f;
        private void Move(Vector2 delta)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .1f)); delta /= count;
            Vector2 position = hero.position;
            for (int i = 0; i < count; i++)
            {
                var next = position + new Vector2(delta.x, 0);
                if (DiscoveryMaps.CanStand(next, GateOpen, true)) position = next;
                next = position + new Vector2(0, delta.y);
                if (DiscoveryMaps.CanStand(next, GateOpen, true)) position = next;
            }
            hero.position = position;
        }
        private void Interact()
        {
            if (!Playing) return;
            if (Near(DiscoveryMaps.Tablet))
            {
                if (!Echo) wind.Inspect();
                message = Echo ? "Make a sound beside the echo stone to open the bridge. The toy listens automatically; you can go straight to the stone."
                    : "Shake the mill's gears, turn the north vane left, the south vane right, then steady the mill. Stand beside each mechanism to send its gesture.";
            }
            else if (Near(DiscoveryMaps.Reward) && (Echo ? echo.Collect() : wind.Collect())) message = "A new discovery for the woodland! Press M to choose another adventure.";
            else message = Objective;
        }
        private void Motion(ToyMotion motion)
        {
            if (!Playing || Echo) return;
            if (!Near(wind.Station)) { message = "Go to the mechanism in your objective before sending a gesture."; return; }
            message = wind.Apply(motion) ? "SIMULATED " + motion + " accepted. " + Objective : "That gesture doesn't fit yet. " + Objective;
        }
        private void Listen()
        {
            if (!Playing || !Echo || GateOpen) return;
            if (simulatedSound) { message = "Switch off simulated sound to use the Sense microphone."; return; }
            if (!Near(WindmillQuest.Mill)) { message = "Stand beside the echo stone to listen."; return; }
            if (microphone.Busy) return;
            nextListen = Time.unscaledTime + 2.5f;
            microphone.Listen();
        }
        private void RealSound(ToySoundReading sound) { if (!simulatedSound) ApplySound(sound, "SENSE MIC"); }
        private void Simulate(ToySoundReading sound) { if (simulatedSound) ApplySound(sound, "SIMULATED"); }
        private void ApplySound(ToySoundReading sound, string source)
        {
            if (!Playing || !Echo) return;
            lastSound = $"{source} · RMS {sound.Rms:F4} · peak {sound.Peak:F4}"; pulse = 1;
            bool atStone = Near(WindmillQuest.Mill);
            if (echo.Apply(sound, atStone)) message = "The stone heard you! The bridge is open. Walk right and collect the silver bell (E).";
            else if (!atStone) message = "Walk to the glowing echo stone, then make a sound.";
            else if (!GateOpen) message = "Still listening — clap or speak near the toy to open the bridge.";
        }
        private void BuildWorld()
        {
            art.Shape("Meadow", Vector2.zero, new Vector2(23, 13), AdventureArt.Hex(Echo ? "294B48" : "627868"), 0);
            art.Shape("Trail", Vector2.zero, new Vector2(22, 1.5f), AdventureArt.Hex(Echo ? "55746C" : "A39776"), 1);
            for (int i = 0; i < 12; i++)
                art.Shape("Grove", new Vector2(-10 + i * 1.8f, i % 2 == 0 ? 4.5f : -4.6f), new Vector2(1.6f, 2), AdventureArt.Hex(Echo ? "355D58" : "789083"), 2, true);
            art.Shape("Journal", DiscoveryMaps.Tablet, new Vector2(.8f, .65f), Accent, 5);
            art.Shape("Mill tower", WindmillQuest.Mill, new Vector2(1.4f, 2.2f), AdventureArt.Hex("7C8D81"), 3);
            stone = art.Shape("Echo stone", WindmillQuest.Mill, Vector2.one * 1.3f, Accent, 6, true);
            sails = new GameObject("Wind sails").transform; sails.SetParent(transform, false); sails.position = new Vector2(-2, .5f);
            for (int i = 0; i < 4; i++)
            {
                var blade = art.Shape("Sail", Vector2.zero, new Vector2(.35f, Echo ? .8f : 1.7f), Accent, 7);
                blade.transform.SetParent(sails, false);
                blade.transform.localPosition = Quaternion.Euler(0, 0, i * 90) * new Vector3(0, Echo ? .6f : 1, 0);
                blade.transform.localRotation = Quaternion.Euler(0, 0, i * 90);
            }
            if (!Echo)
            {
                art.Shape("North vane", WindmillQuest.Left, new Vector2(1.1f, 1.1f), Accent, 6, true);
                art.Shape("South vane", WindmillQuest.Right, new Vector2(1.1f, 1.1f), Accent, 6, true);
            }
            art.Shape("Ravine", new Vector2(5.7f, 0), new Vector2(.8f, 14), AdventureArt.Hex("243A43"), 4);
            gate = art.Shape("Wind bridge", new Vector2(5.7f, 0), new Vector2(1, 1.8f), Accent, 5);
            art.Shape("Reward dais", DiscoveryMaps.Reward, new Vector2(2, 1.1f), AdventureArt.Hex("8BA49D"), 5, true);
            art.Shape("Relic", DiscoveryMaps.Reward + Vector2.up * .5f, new Vector2(.5f, 1), Accent, 7, true);
            hero = art.Explorer(DiscoveryMaps.Spawn);
        }
        private void Label(Vector2 point, string text)
        {
            var screen = art.Camera.WorldToScreenPoint(point);
            var rect = new Rect(screen.x / Screen.width * 1280 - 100, (1 - screen.y / Screen.height) * 800 - 14, 200, 28);
            AdventureGui.Panel(rect, AdventureArt.Hex("203A43")); GUI.Label(rect, text, gui.Tag);
        }
        private void OnGUI()
        {
            if (art == null) return;
            if (gui == null) gui = new AdventureGui();
            var previous = AdventureGui.Begin();
            var panel = AdventureArt.Hex("172F38");
            AdventureGui.Panel(new Rect(0, 0, 1280, 124), panel);
            GUI.Label(new Rect(30, 14, 900, 40), "ARdventure  /  " + (Echo ? "Echo Grove" : "Windmill Ridge"), gui.Heading);
            GUI.Label(new Rect(30, 59, 970, 36), Objective, gui.Body);
            GUI.Label(new Rect(30, 99, 970, 24), "WASD / arrows: move   E: interact   Esc: pause   R: restart", gui.Small);
            GUI.Label(new Rect(1010, 24, 240, 45), Echo ? "05 / THE SILVER BELL" : "04 / THE SKY FEATHER", gui.Small);
            if (!Echo) Label(DiscoveryMaps.Tablet + Vector2.up * 1.2f, "KEEPER'S JOURNAL");
            Label(WindmillQuest.Mill + Vector2.up * 2, Echo ? "ECHO STONE" : "WINDMILL · X / V");
            if (!Echo) { Label(WindmillQuest.Left + Vector2.up * 1.2f, "NORTH VANE · Z"); Label(WindmillQuest.Right + Vector2.up * 1.2f, "SOUTH VANE · C"); }
            Label(DiscoveryMaps.Reward + Vector2.up * 1.8f, Echo ? "SILVER BELL" : "SKY FEATHER");
            GUI.enabled = Playing;
            if ((!Echo && Near(DiscoveryMaps.Tablet)) || (GateOpen && Near(DiscoveryMaps.Reward)))
                if (GUI.Button(new Rect(440, 552, 400, 38), "E · " + (Near(DiscoveryMaps.Tablet) ? "Read journal" : "Collect relic"), gui.Button)) Interact();
            GUI.enabled = true;
            AdventureGui.Panel(new Rect(0, 612, 1280, 188), panel);
            GUI.Label(new Rect(30, 628, 740, 68), message, gui.Body);
            if (Echo) DrawSound(); else DrawMotion();
            if (paused || Complete)
            {
                AdventureGui.Panel(new Rect(330, 280, 620, 225), panel);
                GUI.Label(new Rect(365, 305, 550, 45), paused ? "Taking a breather" : "Adventure complete!", gui.Heading);
                GUI.Label(new Rect(365, 365, 550, 55), paused ? "Your discoveries will be here when you're ready." : "Press M to choose another adventure, or play again.", gui.Body);
                if (GUI.Button(new Rect(365, 442, 260, 40), paused ? "Continue adventure" : "Play again", gui.Button)) { if (paused) Pause(); else Restart(); }
            }
            EditingAddress = Echo && showConnection && !simulatedSound && GUI.GetNameOfFocusedControl() == "SoundAddress";
            GUI.matrix = previous;
        }
        private void DrawMotion()
        {
            GUI.Label(new Rect(30, 720, 720, 55), "SIMULATED GESTURES · No motion sensor is fitted.\nThese inputs can later come from a separate IMU adapter.", gui.Small);
            GUI.enabled = Playing;
            string[] names = { "X · Shake", "Z · Tilt left", "C · Tilt right", "V · Steady" };
            for (int i = 0; i < 4; i++) if (GUI.Button(new Rect(810 + i % 2 * 220, 635 + i / 2 * 60, 205, 44), names[i], gui.Button)) motionInput.Send((ToyMotion)i);
            GUI.enabled = true;
        }
        private void DrawSound()
        {
            bool atStone = Near(WindmillQuest.Mill);
            string prompt = GateOpen ? "SOUND HEARD · PATH OPEN" : !atStone ? "1 · WALK TO THE GLOWING STONE"
                : simulatedSound ? "2 · PRESS O TO MAKE A SOUND" : "2 · CLAP OR SPEAK NEAR THE TOY";
            GUI.Label(new Rect(810, 626, 425, 32), prompt, gui.Small);
            GUI.enabled = Playing && !GateOpen && atStone && !microphone.Busy;
            if (simulatedSound)
            {
                if (GUI.Button(new Rect(810, 665, 425, 42), "O · Make a sound", gui.Button)) Simulate(ToySoundReading.Call);
            }
            else if (GUI.Button(new Rect(810, 665, 425, 42), microphone.Busy ? "Listening... make a sound!" : "Listen again (I or L)", gui.Button)) { Listen(); GUI.FocusControl(null); }
            GUI.enabled = true;
            if (GUI.Button(new Rect(810, 723, 425, 35), simulatedSound ? "Input: simulated · F2 to use Sense mic" : "Input: Sense mic · F2 to simulate", gui.Button)) SetSoundMode(!simulatedSound);
            GUI.Label(new Rect(30, 710, 740, 38), GateOpen ? "One sound was enough. Go and claim the silver bell!" : simulatedSound ? "Simulation selected. Press O beside the stone."
                : atStone ? microphone.Status : "The Sense microphone starts listening when you reach the stone.", gui.Small);
            showConnection = GUI.Toggle(new Rect(30, 759, 210, 24), showConnection, "Connection settings", gui.Small);
            if (showConnection && !simulatedSound)
            {
                GUI.SetNextControlName("SoundAddress");
                var address = GUI.TextField(new Rect(240, 754, 340, 28), ToyConnectionSettings.Address, 100);
                if (address != ToyConnectionSettings.Address) { microphone.CancelPending(); ToyConnectionSettings.Address = address; nextListen = 0; }
                GUI.Label(new Rect(590, 757, 650, 30), lastSound, gui.Small);
            }
        }
        private void SetSoundMode(bool simulated)
        {
            simulatedSound = simulated; microphone.CancelPending(); nextListen = 0;
            lastSound = "Input changed";
            message = GateOpen ? "The bridge is open. Collect the silver bell (E)." : "Make a sound beside the echo stone to open the bridge.";
        }
    }
}
