using UnityEngine;
using UnityEngine.InputSystem;

namespace ARdventure
{
    public sealed class DiscoveryQuestScreen : MonoBehaviour
    {
        [HideInInspector] public bool Moon;
        private readonly LanternQuest lantern = new LanternQuest();
        private readonly MoonlightQuest moon = new MoonlightQuest();
        private AdventureArt art;
        private AdventureGui gui;
        private SimulatedToyInput simulator;
        private WifiToyInput cameraInput;
        private Transform hero;
        private SpriteRenderer gate, reward;
        private readonly SpriteRenderer[] lights = new SpriteRenderer[3];
        private bool paused, useCamera;
        public bool EditingAddress { get; private set; }
        public bool Complete => Moon ? moon.Stage == MoonStage.Complete : lantern.Complete;
        private bool GateOpen => Moon ? moon.GateOpen : lantern.GateOpen;
        private string message;
        private string Title => Moon ? "Moonlit Ruins" : "Prism Hollow";
        private string Objective => Moon ? moon.Objective : lantern.Objective;
        private Color Accent => AdventureArt.Hex(Moon ? "BDB4EC" : "F4CD88");

        private void Start()
        {
            simulator = gameObject.AddComponent<SimulatedToyInput>();
            simulator.ColorScanned += Scan;
            cameraInput = gameObject.AddComponent<WifiToyInput>();
            cameraInput.ColorScanned += Scan;
            art = new AdventureArt(transform, AdventureArt.Hex(Moon ? "172A3B" : "222334"));
            BuildWorld();
            Restart();
        }
        private void OnDisable()
        {
            if (simulator != null) simulator.Enabled = false;
            if (cameraInput != null) cameraInput.AcceptingScans = false;
        }
        private void OnDestroy()
        {
            if (simulator != null) simulator.ColorScanned -= Scan;
            if (cameraInput != null) cameraInput.ColorScanned -= Scan;
            art?.Dispose();
        }
        private void Update()
        {
            var keys = EditingAddress ? null : Keyboard.current;
            if (keys != null && keys.escapeKey.wasPressedThisFrame) paused = !paused;
            if (keys != null && keys.rKey.wasPressedThisFrame) Restart();
            simulator.Enabled = !paused && !Complete;
            simulator.KeyboardEnabled = !EditingAddress;
            cameraInput.AcceptingScans = useCamera && !paused && !Complete;
            if (!paused && !Complete && keys != null)
            {
                var movement = new Vector2(
                    ((keys.dKey.isPressed || keys.rightArrowKey.isPressed) ? 1 : 0) - ((keys.aKey.isPressed || keys.leftArrowKey.isPressed) ? 1 : 0),
                    ((keys.wKey.isPressed || keys.upArrowKey.isPressed) ? 1 : 0) - ((keys.sKey.isPressed || keys.downArrowKey.isPressed) ? 1 : 0));
                Move(movement.normalized * (3.5f * Time.deltaTime));
                if (keys.eKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame) Interact();
                if (keys.tKey.wasPressedThisFrame) ScanCamera();
            }
            gate.color = GateOpen ? new Color(Accent.r, Accent.g, Accent.b, 0.16f) : AdventureArt.Hex("7A698C");
            for (int i = 0; i < 3; i++)
            {
                bool lit = Moon ? (i == 2 ? moon.RunesMatched >= 1 : i == 0 ? moon.RunesMatched >= 2 : moon.RunesMatched >= 3) : lantern.LitCount > i;
                lights[i].color = lit ? (Moon ? Accent : LanternColour(i)) : AdventureArt.Hex("52616B");
                lights[i].transform.localScale = Vector2.one * (lit ? 0.95f : 0.6f);
            }
            reward.transform.localScale = Vector2.one * (Complete ? 1.3f : 0.85f);
            art.FitCamera();
        }
        private void Move(Vector2 delta)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 0.1f));
            delta /= count;
            Vector2 position = hero.position;
            for (int i = 0; i < count; i++)
            {
                var next = position + new Vector2(delta.x, 0);
                if (DiscoveryMaps.CanStand(next, GateOpen, Moon)) position = next;
                next = position + new Vector2(0, delta.y);
                if (DiscoveryMaps.CanStand(next, GateOpen, Moon)) position = next;
            }
            hero.position = position;
        }
        private bool Near(Vector2 point) => Vector2.Distance(hero.position, point) < 1.5f;
        private int NearbyNode()
        {
            var positions = Moon ? DiscoveryMaps.Runes : DiscoveryMaps.Lanterns;
            for (int i = 0; i < positions.Length; i++) if (Near(positions[i])) return i;
            return -1;
        }
        private string Interaction()
        {
            if (Near(DiscoveryMaps.Tablet)) return Moon ? "Inspect the moon seal" : "Read the lantern tablet";
            int node = NearbyNode();
            if (node >= 0) return Moon ? "Touch " + RuneName(node) + " rune" : "Inspect " + LanternQuest.ColourName(node).ToLowerInvariant() + " lantern";
            return Near(DiscoveryMaps.Reward) ? (Moon ? "Collect the moonstone" : "Claim the guiding star") : "";
        }
        private void Interact()
        {
            if (paused || Complete) return;
            if (Near(DiscoveryMaps.Tablet))
            {
                if (Moon)
                {
                    moon.InspectSeal();
                    message = "The seal reads: 'What daylight hides, shadow remembers.' Shade the camera and scan (T), or simulate shade with N.";
                }
                else
                {
                    lantern.InspectTablet();
                    message = "The tablet reads: BLUE, then GOLD, then GREEN. Visit each lantern and scan its colour to push back the shadows.";
                }
                return;
            }
            int node = NearbyNode();
            if (node >= 0)
            {
                if (!Moon)
                    message = !lantern.Started ? "Read the entrance tablet to learn how these lanterns work."
                        : node < lantern.LitCount ? "This lantern is awake. " + lantern.Objective
                        : "This is the " + LanternQuest.ColourName(node).ToLowerInvariant() + " lantern. " + lantern.Objective;
                else if (moon.Stage != MoonStage.SolveRunes)
                    message = "A " + RuneName(node) + " rune. " + moon.Objective;
                else if (moon.ChooseRune(node))
                    message = moon.GateOpen ? "The stones sing together! The arch is open. Go and collect the moonstone."
                        : "The rune hums. " + moon.RunesMatched + " of 3 remembered.";
                else message = "That wasn't the next rune. Sequence reset. Shade and scan again if you need to see the clue.";
                return;
            }
            if (Near(DiscoveryMaps.Reward))
            {
                bool completed = Moon ? moon.CollectRelic() : lantern.ReachExit();
                if (completed) message = Moon ? "The moonstone is safe. The woodland has a new story to tell!" : "You restored the guiding star! Travellers can find their way through Prism Hollow again.";
            }
        }
        private void Scan(Color colour)
        {
            if (paused || Complete) return;
            if (Moon)
            {
                if (moon.Scan(colour))
                    message = moon.Stage == MoonStage.FindLight
                        ? "The hidden order is visible above the seal. Remember it, then uncover the camera and scan in the light (or L)."
                        : "The clue has faded. Touch the rune stones in the order you saw. Shade and scan again for a reminder.";
                else message = moon.Stage == MoonStage.InspectSeal ? "No discovery mission yet. Inspect the moon seal first."
                    : moon.Stage == MoonStage.FindShade ? "Still too bright. Shade the lens gently and scan again, or press N."
                    : moon.Stage == MoonStage.FindLight ? "More light is needed. Uncover the lens, aim at a well-lit surface and scan, or press L."
                    : moon.Objective;
            }
            else
            {
                int node = NearbyNode();
                if (lantern.Scan(node, colour))
                    message = lantern.GateOpen ? "All three lanterns are awake! Cross the glowing arch and claim the guiding star."
                        : "The lantern wakes! " + lantern.Objective + ".";
                else message = !lantern.Started ? "No discovery mission yet. Read the entrance tablet first."
                    : lantern.GateOpen ? lantern.Objective
                    : node != lantern.LitCount ? "Bring your discovery to the " + LanternQuest.ColourName(lantern.LitCount).ToLowerInvariant() + " lantern."
                    : "That colour doesn't match. Find " + LanternQuest.ColourName(node).ToLowerInvariant() + " and scan again.";
            }
        }
        private void ScanCamera()
        {
            useCamera = true; cameraInput.AcceptingScans = true; cameraInput.Scan();
        }
        private void Restart()
        {
            cameraInput.CancelPending(); lantern.Reset(); moon.Reset(); paused = false;
            hero.position = DiscoveryMaps.Spawn;
            message = Moon ? "An old moon seal guards a lost relic. Walk up to the carved seal and inspect it."
                : "The lanterns of Prism Hollow have gone out. Read the tablet beside the entrance to discover their secret.";
        }
        private static string RuneName(int index) => index == 0 ? "MOON" : index == 1 ? "LEAF" : "STAR";
        private static Color LanternColour(int index) => AdventureArt.Hex(index == 0 ? "70BFE9" : index == 1 ? "EEC45D" : "9BD880");

        private void BuildWorld()
        {
            art.Shape("Ground", Vector2.zero, new Vector2(24, 15), AdventureArt.Hex(Moon ? "263D4B" : "383449"));
            art.Shape("Central trail", Vector2.zero, new Vector2(21, 1.5f), AdventureArt.Hex(Moon ? "6C8290" : "817482"), 2);
            for (int i = 0; i < 14; i++)
            {
                var colour = AdventureArt.Hex(Moon ? "3E596A" : "4F4868");
                art.Shape("Border rock", new Vector2(-11 + i * 1.7f, 5.5f), new Vector2(1.4f, 1.2f), colour, 3, true);
                art.Shape("Border rock", new Vector2(-11 + i * 1.7f, -5.5f), new Vector2(1.8f, 1.2f), colour, 3, true);
            }
            art.Shape("Entrance tablet", DiscoveryMaps.Tablet, new Vector2(1.1f, 0.9f), Accent, 8);
            var positions = Moon ? DiscoveryMaps.Runes : DiscoveryMaps.Lanterns;
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = positions[i];
                art.Shape("Branch path", new Vector2(p.x, p.y * 0.5f), new Vector2(1.1f, Mathf.Abs(p.y) + 1), AdventureArt.Hex(Moon ? "6C8290" : "817482"), 2);
                art.Shape("Stone plinth", p, new Vector2(1.25f, 0.8f), AdventureArt.Hex("A1AAA7"), 6, true);
                art.Shape("Pillar", p + Vector2.up * 0.4f, new Vector2(0.45f, 1), AdventureArt.Hex("70838D"), 7);
                lights[i] = art.Shape("Glow", p + Vector2.up, Vector2.one * 0.6f, AdventureArt.Hex("52616B"), 8, true);
            }
            float gateX = Moon ? 5.7f : 4.2f;
            art.Shape("Shadow wall", new Vector2(gateX, 0), new Vector2(0.7f, 13.5f), AdventureArt.Hex("202233"), 4);
            gate = art.Shape("Sealed arch", new Vector2(gateX, 0), new Vector2(0.8f, 2), AdventureArt.Hex("7A698C"), 9);
            art.Shape("Arch north", new Vector2(gateX, 1.35f), Vector2.one * 0.8f, Accent, 10);
            art.Shape("Arch south", new Vector2(gateX, -1.35f), Vector2.one * 0.8f, Accent, 10);
            art.Shape("Reward dais", DiscoveryMaps.Reward, new Vector2(2, 1.2f), AdventureArt.Hex("9BA9AA"), 5, true);
            reward = art.Shape("Quest relic", DiscoveryMaps.Reward + Vector2.up * 0.5f, Vector2.one * 0.85f, Accent, 12, Moon);
            if (!Moon) reward.transform.rotation = Quaternion.Euler(0, 0, 45);
            hero = art.Explorer(DiscoveryMaps.Spawn);
        }

        private void OnGUI()
        {
            if (art == null) return;
            if (gui == null) gui = new AdventureGui();
            var previous = AdventureGui.Begin();
            var panel = AdventureArt.Hex("172B36");
            AdventureGui.Panel(new Rect(0, 0, 1280, 124), panel);
            GUI.Label(new Rect(30, 14, 900, 40), "ARdventure  /  " + Title, gui.Heading);
            GUI.Label(new Rect(30, 59, 970, 32), Objective, gui.Body);
            GUI.Label(new Rect(30, 97, 960, 22), "WASD / arrows: move   E / Space: interact   T: scan toy   Esc: pause   R: restart", gui.Small);
            GUI.Label(new Rect(1010, 24, 240, 45), Moon ? "03  /  THE HIDDEN SEAL" : "02  /  SLEEPING LANTERNS", gui.Small);
            int step = Moon ? (int)moon.Stage : Complete ? 5 : !lantern.Started ? 0 : lantern.LitCount + 1;
            for (int i = 0; i < 6; i++) AdventureGui.Panel(new Rect(1010 + i * 38, 76, 29, 6), i <= step ? Accent : AdventureArt.Hex("36505E"));
            WorldLabel(DiscoveryMaps.Tablet + Vector2.up * 1.2f, Moon ? "MOON SEAL" : "LANTERN TABLET");
            var positions = Moon ? DiscoveryMaps.Runes : DiscoveryMaps.Lanterns;
            for (int i = 0; i < 3; i++) WorldLabel(positions[i] + Vector2.up * 1.65f, Moon ? RuneName(i) : LanternQuest.ColourName(i));
            WorldLabel(DiscoveryMaps.Reward + Vector2.up * 1.9f, Moon ? "MOONSTONE" : "GUIDING STAR");
            if (Moon && moon.Stage == MoonStage.FindLight)
            {
                AdventureGui.Panel(new Rect(370, 210, 540, 75), panel);
                GUI.Label(new Rect(380, 216, 520, 22), "THE SHADOW REMEMBERS", gui.Tag);
                GUI.Label(new Rect(380, 246, 520, 32), MoonlightQuest.Clue, gui.Heading);
            }
            string action = Interaction();
            if (!paused && !Complete && action.Length > 0 && GUI.Button(new Rect(430, 550, 420, 40), "E · " + action, gui.Button)) Interact();
            AdventureGui.Panel(new Rect(0, 612, 1280, 188), panel);
            GUI.Label(new Rect(30, 629, 760, 76), message, gui.Body);
            DrawToyControls();
            if (paused || Complete)
            {
                AdventureGui.Panel(new Rect(330, 290, 620, 220), panel);
                GUI.Label(new Rect(365, 315, 550, 45), paused ? "Taking a breather" : "Adventure complete!", gui.Heading);
                GUI.Label(new Rect(365, 371, 550, 50), paused ? "Your discoveries will be here when you're ready." : "A new discovery for the woodland. Press M to choose another adventure.", gui.Body);
                if (GUI.Button(new Rect(365, 445, 260, 40), paused ? "Continue adventure" : "Play again", gui.Button))
                { if (paused) paused = false; else Restart(); }
            }
            EditingAddress = useCamera && GUI.GetNameOfFocusedControl() == "DiscoveryToyAddress";
            GUI.matrix = previous;
        }
        private void DrawToyControls()
        {
            useCamera = GUI.Toggle(new Rect(30, 716, 230, 28), useCamera, useCamera ? "[x] Toy camera enabled" : "[ ] Enable toy camera", gui.Small);
            if (useCamera)
            {
                GUI.SetNextControlName("DiscoveryToyAddress");
                var address = GUI.TextField(new Rect(270, 712, 300, 30), cameraInput.Address, 100);
                if (address != cameraInput.Address) { cameraInput.CancelPending(); cameraInput.Address = address; }
                GUI.enabled = !paused && !Complete && !cameraInput.Busy;
                if (GUI.Button(new Rect(590, 707, 200, 40), "T · Scan toy", gui.Button)) { ScanCamera(); GUI.FocusControl(null); }
                GUI.enabled = true;
                GUI.Label(new Rect(30, 756, 760, 26), cameraInput.Status, gui.Small);
            }
            else { cameraInput.AcceptingScans = false; GUI.Label(new Rect(30, 756, 760, 26), "Camera optional · All discoveries can also be simulated", gui.Small); }
            GUI.Label(new Rect(840, 627, 410, 24), "SIMULATED DISCOVERIES", gui.Small);
            GUI.enabled = !paused && !Complete;
            if (Moon)
            {
                if (GUI.Button(new Rect(840, 660, 192, 40), "N · Shade", gui.Button)) simulator.ScanDark();
                if (GUI.Button(new Rect(1048, 660, 192, 40), "L · Light", gui.Button)) simulator.ScanLight();
            }
            else
            {
                if (GUI.Button(new Rect(840, 660, 123, 40), "B · Blue", gui.Button)) simulator.ScanBlue();
                if (GUI.Button(new Rect(978, 660, 123, 40), "Y · Gold", gui.Button)) simulator.ScanGold();
                if (GUI.Button(new Rect(1116, 660, 124, 40), "G · Green", gui.Button)) simulator.ScanGreen();
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(840, 719, 192, 40), paused ? "Resume" : "Pause", gui.Button)) paused = !paused;
            if (GUI.Button(new Rect(1048, 719, 192, 40), "Restart quest", gui.Button)) Restart();
        }
        private void WorldLabel(Vector2 point, string text)
        {
            var screen = art.Camera.WorldToScreenPoint(point);
            var rect = new Rect(screen.x / Screen.width * 1280 - 80, (1 - screen.y / Screen.height) * 800 - 14, 160, 28);
            AdventureGui.Panel(rect, AdventureArt.Hex("203140"));
            GUI.Label(rect, text, gui.Tag);
        }
    }
}
