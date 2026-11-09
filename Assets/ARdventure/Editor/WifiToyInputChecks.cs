using System;
using UnityEditor;
using UnityEngine;

namespace ARdventure.Editor
{
    public static class WifiToyInputChecks
    {
        [MenuItem("ARdventure/Validate Wi-Fi Toy Protocol")]
        public static void Validate()
        {
            const string green = "{\"version\":1,\"source\":\"xiao-camera\",\"sequence\":1,\"r\":30,\"g\":200,\"b\":40,\"samples\":19200}";
            Check(WifiToyInput.TryReadColour(green, out var colour), "Accept firmware payload");
            Check(Mathf.Abs(colour.g - 200f / 255f) < 0.001f, "Normalise channels");
            var quest = new ForestQuest();
            Check(!quest.Scan(colour), "Hardware cannot skip introduction");
            quest.TalkToGuide(); quest.InspectCrossing();
            Check(quest.Scan(colour) && !quest.BridgeGrown, "Hardware unlock still needs bridge activation");
            Check(!WifiToyInput.TryReadColour("{}", out _), "Reject missing fields");
            Check(!WifiToyInput.TryReadColour("not json", out _), "Reject malformed JSON");
            Check(!WifiToyInput.TryReadColour(green.Replace("\"r\":30,", ""), out _), "Reject missing channel");
            Check(!WifiToyInput.TryReadColour(green.Replace("200", "300"), out _), "Reject out-of-range channel");
            Check(!WifiToyInput.TryReadColour(green.Replace("\"version\":1", "\"version\":2"), out _), "Reject unknown version");
            Check(!WifiToyInput.TryReadColour(green.Replace("xiao-camera", "other"), out _), "Reject wrong source");
            Check(!WifiToyInput.TryReadColour(green.Replace("19200", "0"), out _), "Reject empty sample");
            Check(WifiToyInput.TryGetEndpoint("http://192.168.4.1", out var endpoint) && endpoint == "http://192.168.4.1/v1/scan", "Build toy endpoint");
            Check(WifiToyInput.TryGetEndpoint("http://127.0.0.1:8765", out _), "Allow local fixture port");
            Check(!WifiToyInput.TryGetEndpoint("file:///tmp/test", out _), "Reject non-HTTP transport");
            Check(!WifiToyInput.TryGetEndpoint("http://user:pass@192.168.4.1", out _), "Reject credentials in address");
            Check(!WifiToyInput.TryGetEndpoint("http://192.168.4.1/other", out _), "Reject unexpected path");
            Debug.Log("ARdventure: all 16 Wi-Fi toy protocol checks passed.");
        }

        private static void Check(bool condition, string description)
        {
            if (!condition) throw new Exception("Wi-Fi toy validation failed: " + description);
        }
    }
}
