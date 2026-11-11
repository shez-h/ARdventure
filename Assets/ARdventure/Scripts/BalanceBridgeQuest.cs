using UnityEngine;

namespace ARdventure
{
    // Normalised forward position and sideways deviation, independent of pointer pixels.
    // Trackpad tracing is a stand-in for balance, not a measurement from an accelerometer.
    public sealed class BalanceBridgeQuest
    {
        public float Progress { get; private set; }
        public float Strain { get; private set; }
        public bool Active { get; private set; }
        public bool Complete => Progress >= 1;
        public int Slips { get; private set; }
        public string Hint { get; private set; } = "Press and hold the explorer, then trace slowly right along the gold line.";
        public bool Begin(float forward, float deviation)
        {
            if (Complete || !Finite(forward) || !Finite(deviation) || Mathf.Abs(forward - Progress) > .045f || Mathf.Abs(deviation) > 1) return false;
            Active = true;
            Hint = "Keep your trace inside the glowing strip. Slow and steady!";
            return true;
        }
        public void Trace(float forward, float deviation, float seconds)
        {
            if (!Active || Complete || !Finite(forward) || !Finite(deviation) || !Finite(seconds) || seconds <= 0) return;
            // Ignore stalls, focus changes and long frames rather than suddenly throwing the player off.
            seconds = Mathf.Min(seconds, .1f);
            if (Mathf.Abs(deviation) > 1)
            {
                Strain = Mathf.Min(1, Strain + seconds / .65f);
                Hint = "Drifting! Move back onto the gold line.";
                if (Strain >= .999f)
                {
                    Progress = Strain = 0; Active = false; Slips++;
                    Hint = "A wobble! You're safe on the bank. Press the explorer and try again.";
                }
                return;
            }
            Strain = Mathf.Max(0, Strain - seconds * 2);
            if (forward - Progress > .11f)
            { Hint = "Slow down — bring your pointer back beside the explorer."; return; }
            // Follow a continuous trace directly. An arbitrary walking speed makes the
            // explorer lag behind and punishes an otherwise straight trackpad gesture.
            Progress = Mathf.Clamp01(Mathf.Max(Progress, forward));
            Hint = "Keep your trace inside the glowing strip. Slow and steady!";
            if (Progress >= .995f)
            { Progress = 1; Active = false; Strain = 0; Hint = "Safely across! The sky feather is yours."; }
        }
        public void Release()
        {
            if (!Active) return;
            Active = false;
            Hint = "Take your time. Press and hold the explorer to continue from here.";
        }
        public void Reset()
        {
            Progress = Strain = 0; Active = false; Slips = 0;
            Hint = "Press and hold the explorer, then trace slowly right along the gold line.";
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
