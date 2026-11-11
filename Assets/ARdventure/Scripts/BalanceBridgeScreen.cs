using UnityEngine;
using UnityEngine.InputSystem;

namespace ARdventure
{
    public sealed class BalanceBridgeScreen : MonoBehaviour
    {
        private readonly BalanceBridgeQuest quest = new BalanceBridgeQuest();
        private AdventureGui gui;
        private Texture2D disc;
        private bool paused;
        private Vector2 pointer;
        private float deviation;
        private const float StartX = 220, EndX = 1060, LineY = 390, HalfWidth = 32;
        public bool Complete => quest.Complete;
        public float CrossingProgress => quest.Progress;
        public bool Tracing => quest.Active;
        public int Slips => quest.Slips;
        public Vector2 ViewportSize { get; private set; }
        private void Awake()
        {
            var cameraObject = new GameObject("River crossing camera"); cameraObject.transform.SetParent(transform, false);
            cameraObject.AddComponent<Camera>().backgroundColor = AdventureArt.Hex("183A43");
            cameraObject.AddComponent<AudioListener>();
            disc = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                disc.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(32 - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32)))));
            disc.Apply();
        }
        private void OnDisable() => quest.Release();
        private void OnApplicationFocus(bool focus) { if (!focus) quest.Release(); }
        private void OnDestroy() { if (disc != null) Destroy(disc); }
        private void Update()
        {
            ViewportSize = new Vector2(Screen.width, Screen.height);
            var keys = Keyboard.current;
            if (keys != null && keys.rKey.wasPressedThisFrame) Restart();
            if (keys != null && keys.escapeKey.wasPressedThisFrame) TogglePause();
            var mouse = Mouse.current;
            if (mouse == null || paused || Complete) { quest.Release(); return; }
            Vector2 raw = mouse.position.ReadValue();
            pointer = new Vector2(raw.x / Mathf.Max(1, Screen.width) * 1280, (1 - raw.y / Mathf.Max(1, Screen.height)) * 800);
            float forward = (pointer.x - StartX) / (EndX - StartX);
            deviation = (pointer.y - LineY) / HalfWidth;
            if (mouse.leftButton.wasPressedThisFrame) quest.Begin(forward, deviation);
            if (mouse.leftButton.isPressed) quest.Trace(forward, deviation, Time.unscaledDeltaTime);
            else quest.Release();
        }
        private void Restart() { quest.Reset(); paused = false; deviation = 0; }
        private void TogglePause() { paused = !paused; quest.Release(); }
        private void Panel(float x, float y, float w, float h, string hex) => AdventureGui.Panel(new Rect(x, y, w, h), AdventureArt.Hex(hex));
        private void Circle(Vector2 centre, float radius, Color colour)
        {
            var previous = GUI.color; GUI.color = colour;
            GUI.DrawTexture(new Rect(centre.x - radius, centre.y - radius, radius * 2, radius * 2), disc);
            GUI.color = previous;
        }
        private void OnGUI()
        {
            if (gui == null) gui = new AdventureGui();
            var previous = AdventureGui.Begin();
            Panel(0, 0, 1280, 800, "183A43");
            Panel(0, 125, 1280, 470, "53745A");
            Panel(350, 125, 580, 470, "286A80");
            for (int i = 0; i < 9; i++)
            {
                float y = 155 + Mathf.Repeat(i * 57 + Time.unscaledTime * 18, 420);
                Panel(375 + i % 3 * 60, y, 400, 3, "3C8094");
            }
            for (int i = 0; i < 10; i++)
                Circle(new Vector2(i < 5 ? 50 + i * 55 : 990 + (i - 5) * 55, i % 2 == 0 ? 200 : 540), 30, AdventureArt.Hex("668966"));
            // The actual bridge is also the tracing surface: no separate minigame overlay to discover.
            Panel(StartX - 40, LineY - 45, EndX - StartX + 80, 90, "503F31");
            for (int i = 0; i < 45; i++) Panel(StartX - 38 + i * 20, LineY - 40, 17, 80, "AC8A5E");
            Panel(StartX - 45, LineY - 50, EndX - StartX + 90, 5, "E5C69A");
            Panel(StartX - 45, LineY + 45, EndX - StartX + 90, 5, "E5C69A");
            AdventureGui.Panel(new Rect(StartX, LineY - HalfWidth, EndX - StartX, HalfWidth * 2), new Color(.48f, .85f, .78f, .28f));
            for (int i = 0; i < 35; i++) Panel(StartX + i * 24, LineY - 2, 15, 4, "F8E1A0");
            Circle(new Vector2(StartX, LineY), 35, AdventureArt.Hex("95C6A7"));
            Circle(new Vector2(EndX, LineY), 35, AdventureArt.Hex("E7CD8D"));
            GUI.Label(new Rect(105, 290, 230, 30), "START HERE", gui.Tag);
            GUI.Label(new Rect(945, 290, 230, 30), "SKY FEATHER", gui.Tag);
            var explorer = new Vector2(Mathf.Lerp(StartX, EndX, quest.Progress), LineY + (quest.Active ? Mathf.Clamp(deviation, -2, 2) * 14 : 0));
            Circle(explorer + new Vector2(0, 10), 22, AdventureArt.Hex("142F3B"));
            Circle(explorer, 21, AdventureArt.Hex("57C1C6"));
            Circle(explorer + new Vector2(0, -19), 14, AdventureArt.Hex("EAC493"));
            Panel(explorer.x - 23, explorer.y - 34, 46, 7, "EBD39B");
            if (quest.Active) Circle(pointer, 5, AdventureArt.Hex(quest.Strain > .2f ? "F2A48A" : "FFFFFF"));
            GUI.Label(new Rect(30, 18, 960, 40), "ARdventure  /  River Crossing", gui.Heading);
            GUI.Label(new Rect(30, 66, 960, 35), Complete ? "Safely across! The sky feather is yours." : "Keep your balance as you cross the river", gui.Body);
            GUI.Label(new Rect(30, 103, 950, 22), "Trackpad or mouse: hold and trace right   ·   Esc: pause   ·   R: restart", gui.Small);
            GUI.Label(new Rect(1010, 24, 240, 45), "04 / THE STEADY CROSSING", gui.Small);
            Panel(0, 595, 1280, 205, "172F38");
            GUI.Label(new Rect(35, 617, 1210, 52), quest.Hint, gui.Body);
            GUI.Label(new Rect(35, 685, 280, 28), $"CROSSING  {quest.Progress:P0}", gui.Small);
            Panel(35, 723, 360, 12, "35535B");
            AdventureGui.Panel(new Rect(35, 723, 360 * quest.Progress, 12), AdventureArt.Hex("E7CD8D"));
            GUI.Label(new Rect(440, 685, 400, 28), quest.Strain > .2f ? "BALANCE · MOVE BACK TO THE LINE" : "BALANCE · STEADY", gui.Small);
            Panel(440, 723, 360, 12, "35535B");
            AdventureGui.Panel(new Rect(440, 723, 360 * (1 - quest.Strain), 12), AdventureArt.Hex(quest.Strain > .2f ? "EFA087" : "94CEAE"));
            GUI.Label(new Rect(35, 763, 1200, 28), "SIMULATED BALANCE · Trackpad tracing stands in for toy steadiness. No accelerometer is connected. Release to rest; press the explorer to continue.", gui.Small);
            if (GUI.Button(new Rect(880, 691, 160, 42), paused ? "Resume" : "Pause", gui.Button)) TogglePause();
            if (GUI.Button(new Rect(1055, 691, 185, 42), "Restart crossing", gui.Button)) Restart();
            if (paused || Complete)
            {
                Panel(340, 200, 600, 115, "172F38");
                GUI.Label(new Rect(365, 218, 550, 40), Complete ? "Adventure complete!" : "Taking a breather", gui.Heading);
                GUI.Label(new Rect(365, 266, 550, 40), Complete ? "M: choose another adventure · R: cross again" : "Resume, then press the explorer to continue tracing.", gui.Small);
            }
            GUI.matrix = previous;
        }
    }
}
