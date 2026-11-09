using System;
using UnityEngine;

namespace ARdventure
{
    public enum ToyMotion { Shake, TiltLeft, TiltRight, Still }
    public interface IToyMotionInput { event Action<ToyMotion> Motion; }
    public interface IToySoundInput { event Action<ToySoundReading> Sound; }
    public readonly struct ToySoundReading
    {
        public readonly float Rms, Peak, Clipped;
        public ToySoundReading(float rms, float peak, float clipped = 0) { Rms = rms; Peak = peak; Clipped = clipped; }
        public bool Valid => Channel(Rms) && Channel(Peak) && Channel(Clipped) && Peak >= Rms;
        private static bool Channel(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;
        public static ToySoundReading Quiet => new ToySoundReading(.001f, .002f);
        public static ToySoundReading Call => new ToySoundReading(.12f, .3f);
    }
    public enum WindmillStage { Inspect, Shake, TiltLeft, TiltRight, Still, Collect, Complete }
    public sealed class WindmillQuest
    {
        public WindmillStage Stage { get; private set; }
        public bool GateOpen => Stage >= WindmillStage.Collect;
        public static readonly Vector2 Mill = new Vector2(-2, 0), Left = new Vector2(1, 2), Right = new Vector2(3, -2);
        public Vector2 Station => Stage == WindmillStage.TiltLeft ? Left : Stage == WindmillStage.TiltRight ? Right : Mill;
        public string Objective => Stage switch {
            WindmillStage.Inspect => "Read the windkeeper's journal by the entrance",
            WindmillStage.Shake => "Visit the windmill and shake its jammed gears (X)",
            WindmillStage.TiltLeft => "Visit the north vane and tilt left (Z)",
            WindmillStage.TiltRight => "Visit the south vane and tilt right (C)",
            WindmillStage.Still => "Return to the windmill and hold it steady (V)",
            WindmillStage.Collect => "The wind bridge is open! Collect the sky feather",
            _ => "The windmill turns and the woodland breathes again!" };
        public bool Inspect() { if (Stage != WindmillStage.Inspect) return false; Stage++; return true; }
        public bool Apply(ToyMotion motion)
        {
            if (Stage < WindmillStage.Shake || Stage > WindmillStage.Still || (int)motion != (int)Stage - 1) return false;
            Stage++; return true;
        }
        public bool Collect() { if (Stage != WindmillStage.Collect) return false; Stage++; return true; }
        public void Reset() => Stage = WindmillStage.Inspect;
    }
    public enum EchoStage { Call, Collect, Complete }
    public sealed class EchoQuest
    {
        public EchoStage Stage { get; private set; }
        // Normalised peak-window RMS: above measured quiet, below measured voice/claps.
        public const float SoundThreshold = .01f;
        public bool GateOpen => Stage >= EchoStage.Collect;
        public string Objective => Stage == EchoStage.Call ? "Walk to the echo stone and make a sound to open the path"
            : Stage == EchoStage.Collect ? "The path is open! Cross the bridge and collect the silver bell (E)"
            : "The silver bell rings through Echo Grove!";
        public bool Apply(ToySoundReading sound, bool atStone)
        {
            if (Stage != EchoStage.Call || !atStone || !sound.Valid || sound.Peak < SoundThreshold) return false;
            Stage = EchoStage.Collect; return true;
        }
        public bool Collect() { if (Stage != EchoStage.Collect) return false; Stage = EchoStage.Complete; return true; }
        public void Reset() => Stage = EchoStage.Call;
    }
}
