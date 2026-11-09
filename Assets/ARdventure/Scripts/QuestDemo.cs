using UnityEngine;
using UnityEngine.InputSystem;

namespace ARdventure
{
    // A single entry scene hosts independent screens; no scene-name strings or build-index coupling.
    public sealed class QuestDemo : MonoBehaviour
    {
        private GameObject activeQuest;
        private ForestMilestone forest;
        private DiscoveryQuestScreen discovery;
        private ActionQuestScreen action;
        private BalanceBridgeScreen balance;
        private GameObject boardCamera;
        private AdventureGui gui;
        private int selection, activeIndex;
        private readonly bool[] completed = new bool[5];
        private static readonly string[] Titles = { "Mosswood", "Prism Hollow", "Moonlit Ruins", "River Crossing", "Echo Grove" };
        private static readonly string[] Subtitles = { "THE LIVING BRIDGE", "THE SLEEPING LANTERNS", "THE HIDDEN SEAL", "THE STEADY CROSSING", "THE SILVER BELL" };
        private static readonly string[] Stories = {
            "A washed-away crossing. A woodland beacon waiting on the far bank. Help Pip bring the forest back to life.",
            "Three sleeping lanterns guard the path through a shadowy cavern. Restore their colours and recover the guiding star.",
            "A moonstone rests behind an ancient seal. Discover what shadow reveals, remember its secret and open the arch.",
            "Trace a steady path across a narrow river bridge and bring home the sky feather.",
            "Visit the glowing echo stone and make a sound near the toy to open the path."
        };
        private static readonly string[] Discoveries = { "Find something GREEN", "Find BLUE, GOLD and GREEN", "Discover SHADE and LIGHT", "Trackpad BALANCE challenge", "Sense MICROPHONE + simulation" };
        private static readonly string[] Hints = { "Explore · Discover · Grow", "Explore · Match · Illuminate", "Reveal · Remember · Solve", "Trace · Balance · Cross", "Approach · Make a sound · Cross" };
        private static readonly string[] Accents = { "A8D18C", "F1C97E", "BDB4EC", "E7C38A", "A0D7DA" };

        private void Awake()
        {
            boardCamera = new GameObject("Quest board camera");
            boardCamera.transform.SetParent(transform, false);
            var camera = boardCamera.AddComponent<Camera>();
            boardCamera.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = AdventureArt.Hex("142A31");
        }
        private void Update()
        {
            var keys = Keyboard.current;
            if (keys == null) return;
            if (activeQuest != null)
            {
                completed[activeIndex] |= forest != null ? forest.Complete : discovery != null ? discovery.Complete : action != null ? action.Complete : balance != null && balance.Complete;
                bool editing = forest != null ? forest.EditingAddress : discovery != null ? discovery.EditingAddress : action != null && action.EditingAddress;
                if (!editing && keys.mKey.wasPressedThisFrame) ShowBoard();
                return;
            }
            if (keys.leftArrowKey.wasPressedThisFrame || keys.aKey.wasPressedThisFrame) selection = (selection + 4) % 5;
            if (keys.rightArrowKey.wasPressedThisFrame || keys.dKey.wasPressedThisFrame) selection = (selection + 1) % 5;
            if (keys.digit1Key.wasPressedThisFrame) Choose(0);
            else if (keys.digit2Key.wasPressedThisFrame) Choose(1);
            else if (keys.digit3Key.wasPressedThisFrame) Choose(2);
            else if (keys.digit4Key.wasPressedThisFrame) Choose(3);
            else if (keys.digit5Key.wasPressedThisFrame) Choose(4);
            else if (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame) Choose(selection);
        }
        public void Choose(int index)
        {
            if (index < 0 || index > 4 || activeQuest != null) return;
            selection = activeIndex = index;
            boardCamera.SetActive(false);
            activeQuest = new GameObject(Titles[index] + " adventure");
            activeQuest.transform.SetParent(transform, false);
            if (index == 0) forest = activeQuest.AddComponent<ForestMilestone>();
            else if (index <= 2) { discovery = activeQuest.AddComponent<DiscoveryQuestScreen>(); discovery.Moon = index == 2; }
            else if (index == 3) balance = activeQuest.AddComponent<BalanceBridgeScreen>();
            else { action = activeQuest.AddComponent<ActionQuestScreen>(); action.Echo = true; }
        }
        public void ShowBoard()
        {
            if (activeQuest == null) return;
            completed[activeIndex] |= forest != null ? forest.Complete : discovery != null ? discovery.Complete : action != null ? action.Complete : balance != null && balance.Complete;
            // Disable first so pending requests, Update and old cameras cannot reach the next screen.
            activeQuest.SetActive(false);
            Destroy(activeQuest);
            activeQuest = null; forest = null; discovery = null; action = null; balance = null;
            boardCamera.SetActive(true);
        }
        private void OnGUI()
        {
            if (gui == null) gui = new AdventureGui();
            int oldDepth = GUI.depth;
            GUI.depth = -100;
            var previous = AdventureGui.Begin();
            if (activeQuest != null)
            {
                if (GUI.Button(new Rect(1030, 95, 220, 26), "M · Quest board", gui.Button)) ShowBoard();
            }
            else DrawBoard();
            GUI.matrix = previous;
            GUI.depth = oldDepth;
        }
        private void DrawBoard()
        {
            AdventureGui.Panel(new Rect(0, 0, 1280, 800), AdventureArt.Hex("142A31"));
            GUI.Label(new Rect(44, 24, 1150, 28), "ARdventure  /  FIELD JOURNAL", gui.Small);
            GUI.Label(new Rect(40, 63, 1180, 64), "Where will your next discovery take you?", gui.Title);
            GUI.Label(new Rect(44, 137, 1150, 30), "Five adventures. Camera discoveries, simulated gestures and the Sense microphone.", gui.Body);
            for (int i = 0; i < Titles.Length; i++)
            {
                float x = 40 + (i % 3) * 405, y = 190 + (i / 3) * 260;
                var accent = AdventureArt.Hex(Accents[i]);
                AdventureGui.Panel(new Rect(x, y, 390, 244), i == selection ? accent : AdventureArt.Hex("385058"));
                AdventureGui.Panel(new Rect(x + 2, y + 2, 386, 240), AdventureArt.Hex("203A43"));
                AdventureGui.Panel(new Rect(x + 2, y + 2, 386, 5), accent);
                GUI.Label(new Rect(x + 18, y + 15, 354, 25), "0" + (i + 1) + " / " + Subtitles[i] + (completed[i] ? "  ✓" : ""), gui.Small);
                GUI.Label(new Rect(x + 18, y + 46, 354, 38), Titles[i], gui.Heading);
                GUI.Label(new Rect(x + 18, y + 91, 354, 65), Stories[i], gui.Small);
                GUI.Label(new Rect(x + 18, y + 162, 354, 27), Discoveries[i], gui.Small);
                if (GUI.Button(new Rect(x + 18, y + 197, 354, 32), (i + 1) + " · " + (completed[i] ? "Play again" : "Begin adventure"), gui.Button)) Choose(i);
            }
            GUI.Label(new Rect(44, 728, 1180, 30), "1–5: choose   ·   Arrows + Enter: select   ·   M: return to this journal", gui.Body);
            GUI.Label(new Rect(44, 770, 1180, 24), "Toy camera: T   ·   Sense microphone: automatic at echo stone   ·   Every adventure also supports explicit simulated inputs.", gui.Small);
        }
    }
}
