using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARdventure.Editor
{
    public static class ForestMilestoneTools
    {
        public const string ScenePath = "Assets/ARdventure/Scenes/Mosswood.unity";

        [MenuItem("ARdventure/Open Mosswood Prototype")]
        public static void OpenPrototype()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (System.IO.File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else CreateScene();
        }

        private static void CreateScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ARdventure/Scenes"))
                AssetDatabase.CreateFolder("Assets/ARdventure", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Mosswood Prototype");
            root.AddComponent<ForestMilestone>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        [MenuItem("ARdventure/Validate First Quest")]
        public static void ValidateQuest()
        {
            var quest = new ForestQuest();
            int transitions = 0;
            quest.Changed += () => transitions++;
            Check(quest.Stage == QuestStage.MeetGuide, "Starts with meeting Pip");
            Check(!quest.Scan(Color.green), "Early scans cannot skip the introduction");
            Check(!quest.GrowBridge(), "Bridge cannot be grown before its ability is earned");
            Check(!quest.ReachBeacon(), "Beacon cannot complete the quest early");
            Check(!ForestMap.CanStand(new Vector2(1, -0.5f), quest.BridgeGrown), "River blocks the bridge route initially");
            Check(quest.TalkToGuide(), "Pip advances the introduction");
            Check(!quest.TalkToGuide(), "Repeated NPC interaction does not skip a stage");
            Check(quest.InspectCrossing(), "Inspecting crossing starts discovery mission");
            Check(!quest.Scan(Color.blue), "Wrong colour does not solve puzzle");
            Check(!quest.Scan(Color.black), "Dark readings do not count as green");
            Check(!quest.Scan(Color.white), "White readings do not count as green");
            Check(quest.Scan(new Color(0.12f, 0.8f, 0.25f)), "Green unlocks growing magic");
            Check(!quest.BridgeGrown, "Scanning alone does not open crossing");
            Check(quest.GrowBridge(), "Using ability grows bridge");
            Check(ForestMap.CanStand(new Vector2(1, -0.5f), quest.BridgeGrown), "Bridge route opens");
            Check(!ForestMap.CanStand(new Vector2(1, 2), quest.BridgeGrown), "Other river routes remain blocked");
            Check(!ForestMap.CanStand(new Vector2(20, 0), true), "Player stays within map");
            Check(!ForestMap.CanStand(new Vector2(1, 0.3f), true), "Bridge edge accounts for player clearance");
            Check(!CanReachBeacon(false), "No route reaches the beacon before growing the bridge");
            Check(CanReachBeacon(true), "Beacon is reachable across the grown bridge");
            Check(quest.ReachBeacon(), "Beacon completes quest");
            Check(transitions == 5, "Exactly five successful state transitions");
            Check(!quest.Scan(Color.green), "Repeated scan does not regress completed quest");
            quest.Reset();
            Check(quest.Stage == QuestStage.MeetGuide && !quest.BridgeGrown, "Restart resets progression and crossing");
            var inputObject = new GameObject("Input check");
            try
            {
                var simulated = inputObject.AddComponent<SimulatedToyInput>();
                int scans = 0;
                IToyInput input = simulated;
                input.ColorScanned += _ => scans++;
                simulated.ScanGreen();
                simulated.ScanBlue();
                Check(scans == 2, "Debug input forwards both colour events through interface");
                simulated.Enabled = false;
                simulated.ScanGreen();
                Check(scans == 2, "Disabled input does not emit events");
            }
            finally { UnityEngine.Object.DestroyImmediate(inputObject); }
            Debug.Log("ARdventure: all 26 quest, crossing and simulated-input checks passed.");
        }

        private static bool CanReachBeacon(bool bridgeGrown)
        {
            // A quarter-unit grid samples the bridge corridor and both blocked riverbanks.
            // Search the entire map, including routes around both ends of the river.
            const float spacing = 0.25f;
            var start = Vector2Int.RoundToInt(ForestMap.Spawn / spacing);
            var target = Vector2Int.RoundToInt(ForestMap.Beacon / spacing);
            var pending = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int> { start };
            var directions = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            pending.Enqueue(start);
            while (pending.Count > 0)
            {
                var point = pending.Dequeue();
                if (point == target) return true;
                foreach (var direction in directions)
                {
                    var next = point + direction;
                    if (ForestMap.CanStand((Vector2)next * spacing, bridgeGrown) && visited.Add(next))
                        pending.Enqueue(next);
                }
            }
            return false;
        }

        private static void Check(bool condition, string description)
        {
            if (!condition) throw new Exception("ARdventure validation failed: " + description);
        }

        public static void BatchValidate()
        {
            try
            {
                ValidateQuest();
                if (!System.IO.File.Exists(ScenePath)) CreateScene();
                Debug.Log("ARdventure: scene ready at " + ScenePath);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }
    }
}
