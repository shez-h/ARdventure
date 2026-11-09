using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ARdventure
{
    public sealed class ForestMilestone : MonoBehaviour
    {
        private ForestQuest quest;
        private SimulatedToyInput simulator;
        private IToyInput toy;
        private WifiToyInput wifiToy;
        private bool useWifiToy;
        private bool editingToyAddress;
        private Camera view;
        private Transform hero;
        private Transform heroBody;
        private Transform bridge;
        private Transform beaconLight;
        private Sprite square;
        private Sprite circle;
        private Material spriteMaterial;
        private readonly List<SpriteRenderer> bridgeParts = new List<SpriteRenderer>();
        private string message = "Welcome to Mosswood! Follow the path and meet Pip.";
        private float bridgeReveal;
        private float walkingTime;
        private bool paused;
        private GUIStyle heading, body, small, button, worldTag;
        private Texture2D pixel;
        public bool Complete => quest != null && quest.Stage == QuestStage.Complete;
        public bool EditingAddress => editingToyAddress;

        private void Awake()
        {
            quest = new ForestQuest();
            simulator = gameObject.AddComponent<SimulatedToyInput>();
            toy = simulator;
            toy.ColorScanned += OnColorScanned;
            wifiToy = gameObject.AddComponent<WifiToyInput>();
            wifiToy.ColorScanned += OnColorScanned;
            CreateWorld();
        }

        private void OnDestroy()
        {
            if (toy != null) toy.ColorScanned -= OnColorScanned;
            if (wifiToy != null) wifiToy.ColorScanned -= OnColorScanned;
            if (square != null) { Destroy(square.texture); Destroy(square); }
            if (circle != null) { Destroy(circle.texture); Destroy(circle); }
            if (spriteMaterial != null) Destroy(spriteMaterial);
            if (pixel != null) Destroy(pixel);
        }

        private void OnDisable()
        {
            if (simulator != null) simulator.Enabled = false;
            if (wifiToy != null) wifiToy.AcceptingScans = false;
        }

        private void Update()
        {
            var keys = editingToyAddress ? null : Keyboard.current;
            if (keys != null && keys.escapeKey.wasPressedThisFrame) paused = !paused;
            simulator.Enabled = !paused && quest.Stage != QuestStage.Complete;
            simulator.KeyboardEnabled = !editingToyAddress;
            wifiToy.AcceptingScans = useWifiToy && !paused && quest.Stage != QuestStage.Complete;
            if (keys != null && keys.rKey.wasPressedThisFrame) Restart();
            if (!paused && quest.Stage != QuestStage.Complete && !editingToyAddress)
            {
                Vector2 movement = Vector2.zero;
                if (keys != null)
                {
                    movement.x = ((keys.dKey.isPressed || keys.rightArrowKey.isPressed) ? 1 : 0)
                               - ((keys.aKey.isPressed || keys.leftArrowKey.isPressed) ? 1 : 0);
                    movement.y = ((keys.wKey.isPressed || keys.upArrowKey.isPressed) ? 1 : 0)
                               - ((keys.sKey.isPressed || keys.downArrowKey.isPressed) ? 1 : 0);
                }
                Move(movement.normalized * (3.5f * Time.deltaTime));
                if (movement.sqrMagnitude > 0) walkingTime += Time.deltaTime * 12;
                heroBody.localPosition = new Vector3(0, 0.1f + Mathf.Sin(walkingTime) * (movement.sqrMagnitude > 0 ? 0.045f : 0), 0);
                if (keys != null && (keys.eKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame)) Interact();
                if (keys != null && keys.tKey.wasPressedThisFrame)
                {
                    useWifiToy = true;
                    wifiToy.AcceptingScans = true;
                    wifiToy.Scan();
                }
            }
            if (quest.BridgeGrown && !paused) bridgeReveal = Mathf.MoveTowards(bridgeReveal, 1, Time.deltaTime * 1.3f);
            bridge.localScale = new Vector3(Mathf.Max(0.001f, bridgeReveal), 1, 1);
            foreach (var part in bridgeParts) part.enabled = quest.BridgeGrown;
            beaconLight.localScale = Vector3.one * (quest.Stage == QuestStage.Complete ? 1.5f + Mathf.Sin(Time.time * 2) * 0.1f : 0.8f);
            UpdateCamera();
        }

        private void Move(Vector2 delta)
        {
            // Substeps prevent river tunnelling, including on very slow frames.
            int count = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 0.1f));
            delta /= count;
            Vector2 position = hero.position;
            for (int i = 0; i < count; i++)
            {
                var horizontal = position + new Vector2(delta.x, 0);
                if (ForestMap.CanStand(horizontal, quest.BridgeGrown)) position = horizontal;
                var vertical = position + new Vector2(0, delta.y);
                if (ForestMap.CanStand(vertical, quest.BridgeGrown)) position = vertical;
            }
            hero.position = position;
        }

        private bool Near(Vector2 target) => Vector2.Distance(hero.position, target) < 1.5f;

        private string InteractionLabel()
        {
            if (Near(ForestMap.Guide)) return "Talk to Pip";
            if (Near(ForestMap.Crossing)) return quest.Stage == QuestStage.GrowBridge ? "Grow the vine bridge" : "Inspect the crossing";
            if (Near(ForestMap.Beacon)) return "Light the beacon";
            return "";
        }

        private void Interact()
        {
            if (paused || quest.Stage == QuestStage.Complete) return;
            if (Near(ForestMap.Guide))
            {
                if (quest.TalkToGuide()) message = "Pip: The river washed our bridge away. Can you find a way to reach the beacon?";
                else message = quest.Stage < QuestStage.FindGreen
                    ? "Pip: Follow the path east. Have a look at the old crossing."
                    : "Pip: Green holds the forest's growing magic. Your toy can help us find it!";
            }
            else if (Near(ForestMap.Crossing))
            {
                if (quest.InspectCrossing()) message = "These roots need growing magic. Find something green! Scan with the toy camera, or use the simulator.";
                else if (quest.GrowBridge()) message = "The roots are growing into a bridge! Follow it across the river to the beacon.";
                else message = quest.Stage == QuestStage.MeetGuide ? "Pip might know what happened here. Go and speak to the forest keeper."
                    : quest.Stage == QuestStage.FindGreen ? "We need green growing magic. Aim the toy at something green and scan, or simulate with G."
                    : "A living bridge! Cross at the centre of the vines.";
            }
            else if (Near(ForestMap.Beacon))
            {
                if (quest.ReachBeacon()) message = "You did it! Your discovery brought life back to Mosswood. Pip's next adventure is waiting...";
            }
        }

        private void OnColorScanned(Color color)
        {
            if (paused || quest.Stage == QuestStage.Complete) return;
            if (quest.Scan(color)) message = "Green discovered! You learned Vineweave. Return to the broken crossing and use your new ability.";
            else if (quest.Stage < QuestStage.FindGreen) message = "Scan recorded, but there's no discovery mission yet. Follow your current objective first.";
            else if (quest.Stage == QuestStage.FindGreen) message = "That reading isn't green enough. Aim at a bright green object in good light and try again.";
            else message = "You already have growing magic. " + quest.Objective + ".";
        }

        private void Restart()
        {
            wifiToy.CancelPending();
            paused = false;
            quest.Reset();
            hero.position = ForestMap.Spawn;
            bridgeReveal = 0;
            simulator.Enabled = true;
            message = "Welcome to Mosswood! Follow the path and meet Pip.";
        }

        private void CreateWorld()
        {
            square = MakeSprite(false);
            circle = MakeSprite(true);
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            spriteMaterial = new Material(shader);
            var cameraObject = new GameObject("Forest Camera");
            cameraObject.transform.SetParent(transform);
            view = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            view.orthographic = true;
            view.backgroundColor = Hex("142D30");
            view.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.transform.position = new Vector3(0, 0, -10);

            Shape("Forest floor", Vector2.zero, new Vector2(24, 15), Hex("284B3F"), 0);
            Shape("West meadow", new Vector2(-5, 0), new Vector2(11, 10), Hex("42634A"), 1, true);
            Shape("East meadow", new Vector2(7, 0.5f), new Vector2(8, 8), Hex("42634A"), 1, true);
            Shape("West trail", new Vector2(-4.5f, -0.5f), new Vector2(9, 1.6f), Hex("AC9C70"), 2);
            Shape("Trail bend", new Vector2(-5.2f, 0.4f), new Vector2(1.6f, 2.5f), Hex("AC9C70"), 2);
            Shape("East trail", new Vector2(5.1f, -0.5f), new Vector2(6.2f, 1.6f), Hex("AC9C70"), 2);
            Shape("Beacon trail", new Vector2(8, 0.5f), new Vector2(1.6f, 2), Hex("AC9C70"), 2);
            Shape("Riverbank", new Vector2(1, 0), new Vector2(3.1f, 15), Hex("739783"), 3);
            Shape("River", new Vector2(1, 0), new Vector2(2, 15), Hex("348E9E"), 4);
            for (int i = 0; i < 22; i++)
            {
                float y = -6.7f + i * 0.61f;
                Shape("Current", new Vector2(i % 2 == 0 ? 0.5f : 1.45f, y), new Vector2(0.55f, 0.055f), Hex("73C3C1"), 5);
            }
            // Seeded decoration leaves the useful routes clear and is stable on every run.
            var random = new System.Random(42);
            for (int i = 0; i < 65; i++)
            {
                float x = (float)random.NextDouble() * 21 - 10.5f;
                float y = (float)random.NextDouble() * 11 - 5.5f;
                if (Mathf.Abs(x - 1) < 2 || Mathf.Abs(y + 0.5f) < 1.2f) continue;
                if (Vector2.Distance(new Vector2(x, y), ForestMap.Guide) < 1.9f || Vector2.Distance(new Vector2(x, y), ForestMap.Beacon) < 2) continue;
                Shape("Meadow flower", new Vector2(x, y), Vector2.one * 0.10f, i % 3 == 0 ? Hex("F6CF79") : Hex("93B77B"), 6, true);
            }
            for (int i = 0; i < 12; i++)
            {
                Tree(new Vector2(-10.8f + i * 1.9f, 5.0f + (i % 2) * 0.25f));
                Tree(new Vector2(-10.8f + i * 1.9f, -5.5f - (i % 2) * 0.25f));
            }
            Tree(new Vector2(-9.8f, 2.8f)); Tree(new Vector2(-8.3f, 3.6f));
            Tree(new Vector2(4.2f, 3.3f)); Tree(new Vector2(6.2f, 4)); Tree(new Vector2(10.3f, 3));
            Tree(new Vector2(-7, -3.7f)); Tree(new Vector2(5, -3.7f)); Tree(new Vector2(9.2f, -3.4f));

            Shape("Old west stump", new Vector2(-0.45f, -0.5f), new Vector2(0.35f, 1.9f), Hex("6B5445"), 8);
            Shape("Old east stump", new Vector2(2.45f, -0.5f), new Vector2(0.35f, 1.9f), Hex("6B5445"), 8);
            bridge = new GameObject("Vine bridge").transform;
            bridge.SetParent(transform);
            bridge.position = new Vector3(1, -0.5f, 0);
            for (int i = 0; i < 11; i++)
            {
                var part = Shape("Living plank", new Vector2(-1.6f + i * 0.32f, 0), new Vector2(0.27f, 1.65f), Hex(i % 2 == 0 ? "8AC773" : "A2D889"), 9, false, bridge);
                bridgeParts.Add(part);
            }
            bridgeParts.Add(Shape("North vine", new Vector2(0, 0.68f), new Vector2(3.6f, 0.12f), Hex("426E3C"), 10, false, bridge));
            bridgeParts.Add(Shape("South vine", new Vector2(0, -0.68f), new Vector2(3.6f, 0.12f), Hex("426E3C"), 10, false, bridge));
            foreach (var part in bridgeParts) part.enabled = false;

            Character("Pip", ForestMap.Guide, Hex("D9AD68"), Hex("C16A52"));
            hero = Character("Explorer", ForestMap.Spawn, Hex("E9BE83"), Hex("5ABDC4"));
            heroBody = hero.Find("Body");
            Shape("Beacon foundation", ForestMap.Beacon, new Vector2(1.9f, 1), Hex("6B7F76"), 10, true);
            Shape("Beacon pedestal", ForestMap.Beacon + new Vector2(0, 0.3f), new Vector2(0.8f, 1.1f), Hex("B5C6B6"), 11);
            beaconLight = Shape("Beacon glow", ForestMap.Beacon + new Vector2(0, 1), Vector2.one, Hex("FFE4A0"), 12, true).transform;
            UpdateCamera();
        }

        private Transform Character(string title, Vector2 position, Color skin, Color coat)
        {
            var root = new GameObject(title).transform;
            root.SetParent(transform);
            root.position = position;
            Shape("Shadow", new Vector2(0, -0.3f), new Vector2(0.85f, 0.3f), new Color(0.06f, 0.13f, 0.11f, 0.45f), 20, true, root);
            var bodyObject = new GameObject("Body").transform;
            bodyObject.SetParent(root, false);
            Shape("Boot left", new Vector2(-0.17f, -0.24f), new Vector2(0.23f, 0.33f), Hex("283B3E"), 21, false, bodyObject);
            Shape("Boot right", new Vector2(0.17f, -0.24f), new Vector2(0.23f, 0.33f), Hex("283B3E"), 21, false, bodyObject);
            Shape("Coat", Vector2.zero, new Vector2(0.7f, 0.65f), coat, 22, true, bodyObject);
            Shape("Face", new Vector2(0, 0.45f), new Vector2(0.56f, 0.56f), skin, 23, true, bodyObject);
            Shape("Hat", new Vector2(0, 0.72f), new Vector2(0.85f, 0.18f), Hex("E5CF94"), 24, false, bodyObject);
            Shape("Eye left", new Vector2(-0.12f, 0.46f), Vector2.one * 0.065f, Hex("283B3E"), 25, true, bodyObject);
            Shape("Eye right", new Vector2(0.12f, 0.46f), Vector2.one * 0.065f, Hex("283B3E"), 25, true, bodyObject);
            return root;
        }

        private void Tree(Vector2 position)
        {
            if (Mathf.Abs(position.x - 1) < 1.8f) return;
            Shape("Tree shadow", position + new Vector2(0.3f, -0.45f), new Vector2(1.8f, 0.6f), Hex("234237"), 7, true);
            Shape("Tree trunk", position, new Vector2(0.35f, 1.1f), Hex("6D6044"), 10);
            Shape("Tree crown", position + new Vector2(0, 0.75f), new Vector2(1.8f, 1.9f), Hex("234F43"), 11, true);
            Shape("Tree highlight", position + new Vector2(-0.3f, 1.05f), new Vector2(1.1f, 1.1f), Hex("326653"), 12, true);
        }

        private SpriteRenderer Shape(string title, Vector2 position, Vector2 scale, Color color, int order, bool round = false, Transform parent = null)
        {
            var item = new GameObject(title);
            item.transform.SetParent(parent == null ? transform : parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = new Vector3(scale.x, scale.y, 1);
            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = round ? circle : square;
            renderer.sharedMaterial = spriteMaterial;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Sprite MakeSprite(bool round)
        {
            int size = round ? 64 : 1;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var colors = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * (size * 0.5f));
                colors[y * size + x] = new Color(1, 1, 1, !round ? 1 : Mathf.Clamp01(size * 0.5f - distance));
            }
            texture.SetPixels(colors); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }

        private void UpdateCamera()
        {
            view.rect = new Rect(0, 0.235f, 1, 0.61f);
            float aspect = Screen.width * view.rect.width / Mathf.Max(1, Screen.height * view.rect.height);
            view.orthographicSize = Mathf.Max(6.7f, 12f / aspect);
        }

        private void Styles()
        {
            if (heading != null) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            heading = new GUIStyle { font = font, fontSize = 30, fontStyle = FontStyle.Bold, normal = { textColor = Hex("F4EAD0") } };
            body = new GUIStyle { font = font, fontSize = 19, wordWrap = true, normal = { textColor = Hex("E2E9DE") } };
            small = new GUIStyle(body) { fontSize = 15, normal = { textColor = Hex("A9C4B8") } };
            worldTag = new GUIStyle(body) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 17, fontStyle = FontStyle.Bold, fixedHeight = 40 };
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
        }

        private void Panel(Rect rect, Color color)
        {
            GUI.color = color; GUI.DrawTexture(rect, pixel); GUI.color = Color.white;
        }

        private void OnGUI()
        {
            if (quest == null) return;
            Styles();
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 800f, 1));
            Panel(new Rect(0, 0, 1280, 124), Hex("142D30"));
            GUI.Label(new Rect(30, 14, 700, 40), "ARdventure  /  Mosswood", heading);
            GUI.Label(new Rect(31, 59, 850, 29), quest.Objective, body);
            GUI.Label(new Rect(31, 95, 900, 22), "WASD / arrows: move     E / Space: interact     Esc: pause     R: restart", small);
            GUI.Label(new Rect(1000, 22, 250, 25), "CHAPTER 01  •  THE LIVING BRIDGE", small);
            int step = (int)quest.Stage;
            for (int i = 0; i < 6; i++) Panel(new Rect(1000 + i * 40, 69, 32, 7), i <= step ? Hex("B4D38A") : Hex("36534C"));

            WorldLabel(ForestMap.Guide + new Vector2(0, 1.3f), "PIP · FOREST KEEPER");
            WorldLabel(ForestMap.Beacon + new Vector2(0, 1.9f), "WOODLAND BEACON");
            if (!quest.BridgeGrown) WorldLabel(new Vector2(1, 1.1f), "BROKEN CROSSING");
            string interaction = InteractionLabel();
            if (!paused && quest.Stage != QuestStage.Complete && interaction.Length > 0)
            {
                Panel(new Rect(440, 547, 400, 42), Hex("193C36"));
                if (GUI.Button(new Rect(445, 549, 390, 38), "E  ·  " + interaction, button)) Interact();
            }
            Panel(new Rect(0, 612, 1280, 188), Hex("142D30"));
            GUI.Label(new Rect(30, 630, 765, 79), message, body);
            useWifiToy = GUI.Toggle(new Rect(30, 715, 240, 25), useWifiToy,
                useWifiToy ? "[x] Toy camera enabled" : "[ ] Enable toy camera", small);
            if (useWifiToy)
            {
                GUI.SetNextControlName("ToyAddress");
                string address = GUI.TextField(new Rect(275, 712, 290, 30), wifiToy.Address, 100);
                if (address != wifiToy.Address) { wifiToy.CancelPending(); wifiToy.Address = address; }
                GUI.enabled = !paused && quest.Stage != QuestStage.Complete && !wifiToy.Busy;
                if (GUI.Button(new Rect(580, 707, 215, 40), "T · Scan toy", button))
                { wifiToy.AcceptingScans = true; wifiToy.Scan(); GUI.FocusControl(null); }
                GUI.enabled = true;
                GUI.Label(new Rect(30, 755, 765, 30), wifiToy.Status, small);
            }
            else
            {
                wifiToy.AcceptingScans = false;
                GUI.Label(new Rect(30, 755, 760, 22), "Prototype • Placeholder artwork • Toy connection optional", small);
            }
            GUI.Label(new Rect(850, 626, 390, 24), "SIMULATED INPUTS (G / B)", small);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = !paused && quest.Stage != QuestStage.Complete;
            if (GUI.Button(new Rect(850, 660, 187, 40), "G  ·  Scan green", button)) simulator.ScanGreen();
            if (GUI.Button(new Rect(1053, 660, 187, 40), "B  ·  Scan blue", button)) simulator.ScanBlue();
            GUI.enabled = wasEnabled;
            if (GUI.Button(new Rect(850, 719, 187, 40), paused ? "Resume" : "Pause", button)) paused = !paused;
            if (GUI.Button(new Rect(1053, 719, 187, 40), "Restart quest", button)) Restart();
            if (paused || quest.Stage == QuestStage.Complete)
            {
                Panel(new Rect(350, 240, 580, 230), Hex("142D30"));
                GUI.Label(new Rect(385, 266, 510, 45), paused ? "Taking a breather" : "Mosswood is growing again", heading);
                GUI.Label(new Rect(385, 324, 510, 65), paused ? "Your adventure will be here when you're ready." : "You found growing magic, restored the crossing and lit the beacon. First quest complete!", body);
                if (GUI.Button(new Rect(385, 411, 240, 40), paused ? "Continue adventure" : "Play again", button))
                { if (paused) paused = false; else Restart(); }
            }
            editingToyAddress = useWifiToy && GUI.GetNameOfFocusedControl() == "ToyAddress";
            GUI.matrix = previous;
        }

        private void WorldLabel(Vector2 world, string text)
        {
            var point = view.WorldToScreenPoint(world);
            float x = point.x / Screen.width * 1280f;
            float y = (1 - point.y / Screen.height) * 800f;
            Panel(new Rect(x - 110, y - 15, 220, 30), new Color(0.08f, 0.18f, 0.18f, 0.88f));
            GUI.Label(new Rect(x - 110, y - 15, 220, 30), text, worldTag);
        }
    }
}
