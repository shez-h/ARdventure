using System;
using UnityEditor;
using UnityEngine;

namespace ARdventure.Editor
{
    public static class ActionQuestChecks
    {
        public static void Validate()
        {
            int checks = 0;
            void Check(bool value, string description) { if (!value) throw new Exception("Action quest check failed: " + description); checks++; }
            var wind = new WindmillQuest();
            Check(!wind.Apply(ToyMotion.Shake) && !wind.Collect(), "Motion cannot bypass journal");
            Check(wind.Inspect() && !wind.Inspect(), "Journal starts once");
            Check(!wind.Apply(ToyMotion.Still) && !wind.Apply((ToyMotion)99), "Wrong or invalid motion ignored");
            Check(wind.Apply(ToyMotion.Shake) && wind.Station == WindmillQuest.Left, "Shake sends player to north vane");
            Check(!wind.Apply(ToyMotion.Shake) && !wind.Apply(ToyMotion.TiltRight), "Repeated and out of order gestures ignored");
            Check(wind.Apply(ToyMotion.TiltLeft) && wind.Station == WindmillQuest.Right, "Left vane first");
            Check(wind.Apply(ToyMotion.TiltRight) && wind.Station == WindmillQuest.Mill, "Right vane sends player back to mill");
            Check(!wind.GateOpen && !wind.Collect(), "Vanes alone do not finish");
            Check(wind.Apply(ToyMotion.Still) && wind.GateOpen, "Steady opens bridge");
            Check(wind.Collect() && !wind.Collect() && !wind.Apply(ToyMotion.Shake), "Relic completes once");
            wind.Reset(); Check(wind.Stage == WindmillStage.Inspect && !wind.GateOpen, "Wind restart resets gate");
            var echo = new EchoQuest();
            Check(echo.Stage == EchoStage.Call && !echo.GateOpen, "Echo starts ready for sound without a journal");
            Check(!echo.Collect(), "Cannot collect before sound");
            Check(!echo.Apply(ToySoundReading.Call, false), "Sound away from stone cannot open path");
            Check(!echo.Apply(ToySoundReading.Quiet, true) && !echo.Apply(new ToySoundReading(0, 0), true), "Quiet does not count as sound");
            Check(!echo.Apply(new ToySoundReading(float.NaN, .1f), true) && !echo.Apply(new ToySoundReading(.2f, .1f), true), "Invalid readings rejected");
            Check(echo.Apply(new ToySoundReading(.0072668f, .0637199f), true) && echo.GateOpen, "One recorded real sound opens path immediately");
            Check(!echo.Apply(ToySoundReading.Call, true) && !echo.Apply(ToySoundReading.Quiet, true) && echo.GateOpen, "Path remains open through later sounds and silence");
            Check(echo.Collect() && echo.Stage == EchoStage.Complete && !echo.Collect(), "Collect bell completes once");
            echo.Reset(); Check(echo.Stage == EchoStage.Call && !echo.GateOpen, "Restart closes path and is immediately ready for sound");
            Check(echo.Apply(ToySoundReading.Call, true) && echo.GateOpen, "One simulated sound also opens path");
            echo.Reset();
            Check(!echo.Apply(new ToySoundReading(.001f, EchoQuest.SoundThreshold - .0001f), true), "Below-threshold sound ignored");
            Check(echo.Apply(new ToySoundReading(.001f, EchoQuest.SoundThreshold), true), "Threshold boundary accepted");
            echo.Reset(); Check(echo.Apply(new ToySoundReading(.5f, .9f, .3f), true), "Loud clipped sound still opens path");
            const string payload = "{\"version\":1,\"source\":\"xiao-microphone\",\"sequence\":1,\"rms\":0.0082502,\"peak_rms\":0.0627389,\"clipped_fraction\":0,\"samples\":32000,\"sample_rate\":16000,\"duration_ms\":2000}";
            Check(MicrophoneToyInput.TryReadSound(payload, out var reading) && Mathf.Approximately(reading.Rms, .0082502f), "Actual measured board payload parses");
            foreach (string bad in new[] { "", "{}", "broken", payload.Replace("xiao-microphone", "xiao-camera"), payload.Replace("32000", "10"), payload.Replace("16000", "8000"), payload.Replace("2000", "1000"), payload.Replace("\"sequence\":1", "\"sequence\":0"), payload.Replace("0.0627389", "-1"), payload.Replace("0.0082502", "2"), payload.Replace("\"clipped_fraction\":0,", ""), payload.Replace("\"rms\":0.0082502,", "") })
                Check(!MicrophoneToyInput.TryReadSound(bad, out _), "Malformed or incomplete microphone payload rejected");
            var obj = new GameObject("Motion adapter checks");
            try
            {
                var input = obj.AddComponent<SimulatedMotionInput>(); int events = 0; input.Motion += _ => events++;
                input.Send(ToyMotion.Shake); input.Enabled = false; input.Send(ToyMotion.Still);
                Check(events == 1, "Disabled motion adapter suppresses events");
                input.Enabled = true; obj.SetActive(false); input.Send(ToyMotion.TiltLeft);
                Check(events == 1, "Inactive motion adapter suppresses events");
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
            Debug.Log("ARdventure: all " + checks + " motion, microphone and sound-quest checks passed.");
        }
    }
}
