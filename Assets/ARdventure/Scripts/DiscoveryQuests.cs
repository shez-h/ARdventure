using UnityEngine;

namespace ARdventure
{
    public static class ToyColours
    {
        public static bool Green(Color colour) => Hue(colour, 75, 165, 0.25f, 0.25f);
        public static bool Blue(Color colour) => Hue(colour, 190, 260, 0.25f, 0.25f);
        public static bool Gold(Color colour) => Hue(colour, 35, 75, 0.35f, 0.35f);
        public static bool Dark(Color colour) => Mathf.Max(colour.r, Mathf.Max(colour.g, colour.b)) <= 0.18f;
        public static bool Light(Color colour) => Mathf.Max(colour.r, Mathf.Max(colour.g, colour.b)) >= 0.45f;

        private static bool Hue(Color colour, float from, float to, float saturation, float brightness)
        {
            Color.RGBToHSV(colour, out var h, out var s, out var v);
            return h >= from / 360 && h <= to / 360 && s >= saturation && v >= brightness;
        }
    }

    // Measurement comes from IToyInput; models know nothing about USB, HTTP or keyboards.
    public sealed class LanternQuest
    {
        public bool Started { get; private set; }
        public int LitCount { get; private set; }
        public bool Complete { get; private set; }
        public bool GateOpen => LitCount == 3;
        public string Objective => Complete ? "The lantern trail is shining again!"
            : !Started ? "Read the stone tablet beside the entrance"
            : GateOpen ? "Follow the lantern trail through the gate and claim the star"
            : "Visit the " + ColourName(LitCount).ToLowerInvariant() + " lantern and scan its colour";

        public static string ColourName(int index) => index == 0 ? "BLUE" : index == 1 ? "GOLD" : "GREEN";
        public bool InspectTablet()
        {
            if (Started) return false;
            Started = true;
            return true;
        }
        public bool Scan(int lantern, Color colour)
        {
            if (!Started || Complete || GateOpen || lantern != LitCount) return false;
            bool accepted = lantern == 0 ? ToyColours.Blue(colour) : lantern == 1 ? ToyColours.Gold(colour) : ToyColours.Green(colour);
            if (accepted) LitCount++;
            return accepted;
        }
        public bool ReachExit()
        {
            if (!GateOpen || Complete) return false;
            Complete = true;
            return true;
        }
        public void Reset() { Started = false; LitCount = 0; Complete = false; }
    }

    public enum MoonStage { InspectSeal, FindShade, FindLight, SolveRunes, CollectRelic, Complete }

    public sealed class MoonlightQuest
    {
        public MoonStage Stage { get; private set; }
        public int RunesMatched { get; private set; }
        public bool GateOpen => Stage >= MoonStage.CollectRelic;
        public const string Clue = "STAR  >  MOON  >  LEAF";
        public string Objective => Stage switch
        {
            MoonStage.InspectSeal => "Inspect the moon seal beside the entrance",
            MoonStage.FindShade => "Shade the toy camera and scan to reveal the hidden clue",
            MoonStage.FindLight => "Remember the rune order, then uncover the camera and scan in the light",
            MoonStage.SolveRunes => "Touch the rune stones in the order you discovered",
            MoonStage.CollectRelic => "The seal is open! Reach the moonstone beyond the arch",
            _ => "The moonstone has returned to the woodland!"
        };
        public bool InspectSeal()
        {
            if (Stage != MoonStage.InspectSeal) return false;
            Stage = MoonStage.FindShade;
            return true;
        }
        public bool Scan(Color colour)
        {
            if ((Stage == MoonStage.FindShade || Stage == MoonStage.SolveRunes) && ToyColours.Dark(colour))
            { Stage = MoonStage.FindLight; RunesMatched = 0; return true; }
            if (Stage == MoonStage.FindLight && ToyColours.Light(colour))
            { Stage = MoonStage.SolveRunes; return true; }
            return false;
        }
        public bool ChooseRune(int rune)
        {
            if (Stage != MoonStage.SolveRunes || rune < 0 || rune > 2) return false;
            int expected = RunesMatched == 0 ? 2 : RunesMatched == 1 ? 0 : 1;
            if (rune != expected) { RunesMatched = 0; return false; }
            RunesMatched++;
            if (RunesMatched == 3) Stage = MoonStage.CollectRelic;
            return true;
        }
        public bool CollectRelic()
        {
            if (Stage != MoonStage.CollectRelic) return false;
            Stage = MoonStage.Complete;
            return true;
        }
        public void Reset() { Stage = MoonStage.InspectSeal; RunesMatched = 0; }
    }

    public static class DiscoveryMaps
    {
        public static readonly Vector2 Spawn = new Vector2(-8, -1.5f);
        public static readonly Vector2 Tablet = new Vector2(-7, 0);
        public static readonly Vector2[] Lanterns = { new Vector2(-4, 2), new Vector2(-1, -2), new Vector2(2, 2) };
        public static readonly Vector2[] Runes = { new Vector2(-2, 2), new Vector2(1, -2), new Vector2(4, 2) };
        public static readonly Vector2 Reward = new Vector2(8, 0);
        public static bool CanStand(Vector2 point, bool gateOpen, bool moon)
        {
            if (point.x < -10.5f || point.x > 10.5f || point.y < -5.5f || point.y > 5.5f) return false;
            float gate = moon ? 5.7f : 4.2f;
            return Mathf.Abs(point.x - gate) >= 0.55f || (gateOpen && Mathf.Abs(point.y) < 0.85f);
        }
    }
}
