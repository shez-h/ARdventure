using System;
using UnityEngine;

namespace ARdventure
{
    public enum QuestStage { MeetGuide, InspectCrossing, FindGreen, GrowBridge, ReachBeacon, Complete }

    // No device APIs: both a simulated scanner and a future toy adapter use this model.
    public sealed class ForestQuest
    {
        public QuestStage Stage { get; private set; } = QuestStage.MeetGuide;
        public bool BridgeGrown => Stage >= QuestStage.ReachBeacon;
        public event Action Changed;

        public string Objective => Stage switch
        {
            QuestStage.MeetGuide => "Meet Pip, the forest keeper",
            QuestStage.InspectCrossing => "Investigate the broken river crossing",
            QuestStage.FindGreen => "Find something green with your toy",
            QuestStage.GrowBridge => "Return to the crossing and grow a vine bridge",
            QuestStage.ReachBeacon => "Cross the bridge and reach the woodland beacon",
            _ => "The woodland beacon is shining again!"
        };

        public bool TalkToGuide() => Advance(QuestStage.MeetGuide, QuestStage.InspectCrossing);
        public bool InspectCrossing() => Advance(QuestStage.InspectCrossing, QuestStage.FindGreen);
        public bool GrowBridge() => Advance(QuestStage.GrowBridge, QuestStage.ReachBeacon);
        public bool ReachBeacon() => Advance(QuestStage.ReachBeacon, QuestStage.Complete);

        public bool Scan(Color color)
        {
            // Real cameras produce muted greens; hue separates them from cyan/yellow,
            // while saturation and brightness reject neutral or very dark readings.
            return ToyColours.Green(color) && Advance(QuestStage.FindGreen, QuestStage.GrowBridge);
        }

        public void Reset()
        {
            Stage = QuestStage.MeetGuide;
            Changed?.Invoke();
        }

        private bool Advance(QuestStage expected, QuestStage next)
        {
            if (Stage != expected) return false;
            Stage = next;
            Changed?.Invoke();
            return true;
        }
    }

    public static class ForestMap
    {
        public static readonly Vector2 Spawn = new Vector2(-7.5f, -1.5f);
        public static readonly Vector2 Guide = new Vector2(-5.2f, 1.5f);
        public static readonly Vector2 Crossing = new Vector2(-0.75f, -0.5f);
        public static readonly Vector2 Beacon = new Vector2(8f, 1.4f);
        public const float PlayerRadius = 0.3f;

        public static bool CanStand(Vector2 point, bool bridgeGrown)
        {
            if (point.x < -10.5f || point.x > 10.5f || point.y < -5.5f || point.y > 5.5f) return false;
            bool inRiver = point.x > -0.3f && point.x < 2.3f;
            bool onBridge = Mathf.Abs(point.y + 0.5f) < 0.65f;
            return !inRiver || (bridgeGrown && onBridge);
        }
    }
}
