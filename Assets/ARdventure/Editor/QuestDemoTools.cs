using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARdventure.Editor
{
    public static class QuestDemoTools
    {
        public const string ScenePath = "Assets/ARdventure/Scenes/QuestBoard.unity";

        [MenuItem("ARdventure/Open Adventure Demo")]
        public static void OpenDemo()
        {
            if (EditorApplication.isPlaying)
            { Debug.LogWarning("Stop Play before opening the adventure demo scene."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (System.IO.File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else
            {
                if (!AssetDatabase.IsValidFolder("Assets/ARdventure/Scenes")) AssetDatabase.CreateFolder("Assets/ARdventure", "Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("ARdventure Quest Demo").AddComponent<QuestDemo>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            // Make the quest board the entry point while preserving every existing build scene.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(scene => scene.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("ARdventure: adventure demo ready. First build scene: " + EditorBuildSettings.scenes[0].path);
        }

        [MenuItem("ARdventure/Validate Adventure Demo")]
        public static void ValidateDemo()
        {
            ForestMilestoneTools.ValidateQuest();
            WifiToyInputChecks.Validate();
            ActionQuestChecks.Validate();
            BalanceBridgeChecks.Validate();
            int checks = 0;
            void Check(bool condition, string description)
            {
                if (!condition) throw new Exception("Adventure demo validation failed: " + description);
                checks++;
            }
            var lantern = new LanternQuest();
            Check(!lantern.Scan(0, Color.blue), "Early lantern scan cannot skip tablet");
            Check(!lantern.ReachExit(), "Cannot finish with closed lantern gate");
            Check(lantern.InspectTablet(), "Tablet starts lantern mission");
            Check(!lantern.InspectTablet(), "Repeated tablet does not restart quest");
            Check(!lantern.Scan(1, Color.yellow), "Cannot light gold before blue");
            Check(!lantern.Scan(-1, Color.blue), "Scanning away from a lantern does not progress");
            Check(!lantern.Scan(0, Color.green), "Wrong lantern colour is rejected");
            Check(!lantern.Scan(0, Color.white), "Neutral does not count as blue");
            Check(lantern.Scan(0, Color.blue) && lantern.LitCount == 1, "Blue lights first lantern");
            Check(!lantern.Scan(0, Color.blue), "Repeat scan cannot light next lantern");
            Check(!lantern.Scan(1, Color.red), "Red does not count as gold");
            Check(lantern.Scan(1, new Color(0.95f, 0.75f, 0.1f)), "Gold lights second lantern");
            Check(!lantern.GateOpen, "Two lanterns do not open gate");
            Check(lantern.Scan(2, new Color32(55, 86, 70, 255)) && lantern.GateOpen, "Real muted green opens final lantern");
            Check(lantern.ReachExit() && lantern.Complete, "Reaching star finishes lantern quest");
            Check(!lantern.ReachExit() && !lantern.Scan(2, Color.green), "Completed lantern quest cannot replay transitions");
            lantern.Reset();
            Check(!lantern.Started && !lantern.GateOpen && !lantern.Complete && lantern.LitCount == 0, "Restart resets lantern quest");

            var moon = new MoonlightQuest();
            Check(!moon.Scan(Color.black) && !moon.Scan(Color.white), "Early light scans cannot skip seal");
            Check(!moon.ChooseRune(2) && !moon.CollectRelic(), "Runes/relic cannot skip introduction");
            Check(moon.InspectSeal() && !moon.InspectSeal(), "Seal starts a single discovery mission");
            Check(!moon.Scan(Color.green), "Bright green is not shade");
            Check(moon.Scan(new Color(0.04f, 0.04f, 0.04f)) && moon.Stage == MoonStage.FindLight, "Shade reveals clue");
            Check(!moon.ChooseRune(2), "Cannot solve while clue is visible");
            Check(!moon.Scan(Color.black), "Dark cannot satisfy uncover step");
            Check(!moon.Scan(new Color(0.3f, 0.3f, 0.3f)), "Dim light requires brighter surface");
            Check(moon.Scan(new Color(0.8f, 0.8f, 0.8f)) && moon.Stage == MoonStage.SolveRunes, "Light hides clue and enables stones");
            Check(!moon.ChooseRune(-1) && !moon.ChooseRune(3), "Invalid rune IDs are ignored");
            Check(!moon.ChooseRune(0) && moon.RunesMatched == 0, "Wrong first rune resets sequence");
            Check(moon.ChooseRune(2) && moon.RunesMatched == 1, "Star is first");
            Check(!moon.ChooseRune(1) && moon.RunesMatched == 0, "Wrong later rune clears partial sequence");
            Check(moon.ChooseRune(2) && moon.Scan(Color.black) && moon.RunesMatched == 0 && moon.Stage == MoonStage.FindLight, "Shade can reveal clue again without keeping partial solution");
            Check(moon.Scan(Color.white), "Uncover resumes puzzle after reminder");
            Check(moon.ChooseRune(2) && moon.ChooseRune(0) && moon.ChooseRune(1) && moon.GateOpen, "Star moon leaf opens seal");
            Check(!moon.ChooseRune(2) && !moon.Scan(Color.black), "Solved seal does not regress from later input");
            Check(moon.CollectRelic() && moon.Stage == MoonStage.Complete, "Collecting moonstone completes quest");
            Check(!moon.CollectRelic(), "Completion is idempotent");
            moon.Reset();
            Check(moon.Stage == MoonStage.InspectSeal && !moon.GateOpen && moon.RunesMatched == 0, "Restart clears moon quest and gate");

            foreach (bool moonMap in new[] { false, true })
            {
                Check(!Reachable(point => DiscoveryMaps.CanStand(point, false, moonMap)), "Reward unreachable before gate: " + moonMap);
                Check(Reachable(point => DiscoveryMaps.CanStand(point, true, moonMap)), "Reward reachable after gate: " + moonMap);
                Check(!DiscoveryMaps.CanStand(new Vector2(moonMap ? 5.7f : 4.2f, 3), true, moonMap), "Cannot bypass open arch away from centre");
                Check(!DiscoveryMaps.CanStand(new Vector2(11, 0), true, moonMap), "Map boundary remains blocked");
            }
            var inputObject = new GameObject("Extended simulator checks");
            try
            {
                var input = inputObject.AddComponent<SimulatedToyInput>();
                var readings = new List<Color>();
                input.ColorScanned += readings.Add;
                input.ScanBlue(); input.ScanGold(); input.ScanGreen(); input.ScanDark(); input.ScanLight();
                Check(readings.Count == 5 && ToyColours.Blue(readings[0]) && ToyColours.Gold(readings[1]) && ToyColours.Green(readings[2]) && ToyColours.Dark(readings[3]) && ToyColours.Light(readings[4]), "Five explicit simulator inputs match intended discoveries");
                input.Enabled = false;
                input.ScanGold(); input.ScanDark(); input.ScanLight();
                Check(readings.Count == 5, "Disabled simulator suppresses new discovery types");
            }
            finally { UnityEngine.Object.DestroyImmediate(inputObject); }
            Debug.Log("ARdventure: all " + checks + " expanded-adventure checks passed, plus the existing 31 quest and 16 protocol checks.");
        }

        private static bool Reachable(Func<Vector2, bool> canStand)
        {
            const float spacing = 0.25f;
            var start = Vector2Int.RoundToInt(DiscoveryMaps.Spawn / spacing);
            var target = Vector2Int.RoundToInt(DiscoveryMaps.Reward / spacing);
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int> { start };
            var directions = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                if (point == target) return true;
                foreach (var direction in directions)
                {
                    var next = point + direction;
                    if (canStand((Vector2)next * spacing) && visited.Add(next)) queue.Enqueue(next);
                }
            }
            return false;
        }
    }
}
