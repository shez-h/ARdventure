using System;
using UnityEngine;

namespace ARdventure.Editor
{
    public static class BalanceBridgeChecks
    {
        public static void Validate()
        {
            int checks = 0;
            void Check(bool value, string message) { if (!value) throw new Exception("Bridge balance check failed: " + message); checks++; }
            var quest = new BalanceBridgeQuest();
            Check(!quest.Complete && !quest.Active && quest.Progress == 0, "Crossing starts at bank");
            Check(!quest.Begin(1, 0) && !quest.Begin(0, 2), "Must grab explorer inside the strip");
            Check(!quest.Begin(float.NaN, 0) && !quest.Begin(0, float.PositiveInfinity), "Invalid start input ignored");
            quest.Trace(.1f, 0, .1f); Check(quest.Progress == 0, "Cannot progress without holding");
            Check(quest.Begin(0, 0), "Explorer can be grabbed on bank");
            quest.Trace(1, 0, .1f); Check(quest.Progress == 0, "Jump to far bank cannot skip bridge");
            quest.Trace(.1f, 0, .1f); Check(quest.Progress > 0 && quest.Progress < .02f, "Fast trace is speed limited");
            float progress = quest.Progress;
            quest.Trace(progress + .02f, 1.5f, .1f);
            Check(quest.Strain > 0 && quest.Progress == progress, "Drifting drains balance and stops progress");
            quest.Trace(progress, 0, .1f); Check(quest.Strain == 0, "Returning to line restores balance");
            quest.Release(); quest.Trace(.08f, 0, .1f);
            Check(!quest.Active && quest.Progress == progress, "Releasing pauses in place");
            Check(!quest.Begin(.8f, 0) && quest.Begin(progress, 0), "Resume requires grabbing current explorer");
            quest.Trace(float.NaN, 0, .1f); quest.Trace(.05f, 0, -1);
            Check(quest.Progress == progress, "Invalid trace samples ignored");
            for (int i = 0; i < 7; i++) quest.Trace(progress, 2, .1f);
            Check(quest.Slips == 1 && !quest.Active && quest.Progress == 0 && quest.Strain == 0, "Sustained drift safely returns to bank");
            quest.Begin(0, 0); quest.Trace(.05f, 0, 100);
            Check(quest.Progress < .02f, "Long stalled frame cannot jump ahead");
            for (int i = 0; i < 120 && !quest.Complete; i++) quest.Trace(Mathf.Min(1, quest.Progress + .02f), 0, .1f);
            Check(quest.Complete && !quest.Active && quest.Progress == 1, "Steady continuous trace completes crossing");
            Check(!quest.Begin(1, 0), "Completed crossing cannot restart from input");
            quest.Trace(0, 10, .1f); Check(quest.Complete && quest.Strain == 0, "Later movement cannot undo completion");
            quest.Reset(); Check(!quest.Complete && !quest.Active && quest.Progress == 0 && quest.Slips == 0, "Restart clears crossing");
            Debug.Log("ARdventure: all " + checks + " bridge balance checks passed.");
        }
    }
}
