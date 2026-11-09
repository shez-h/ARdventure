using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ARdventure
{
    // A deliberate two-second capture on the Sense board; never opens the computer microphone.
    public sealed class MicrophoneToyInput : MonoBehaviour, IToySoundInput
    {
        public event Action<ToySoundReading> Sound;
        public string Status { get; private set; } = "The Sense microphone listens automatically at the stone.";
        public bool Busy => active != null;
        private UnityWebRequest active;
        public void Listen()
        {
            if (!isActiveAndEnabled || Busy) return;
            if (!WifiToyInput.TryGetEndpoint(ToyConnectionSettings.Address, out var endpoint))
            { Status = "Enter the toy's HTTP address."; return; }
            StartCoroutine(Capture(endpoint.Replace("/v1/scan", "/v1/listen")));
        }
        public void CancelPending()
        {
            if (active == null) return;
            var cancelled = active; active = null;
            cancelled.Abort(); StopAllCoroutines(); cancelled.Dispose();
            Status = "Listen cancelled. Press I when ready.";
        }
        private void OnDisable() => CancelPending();
        private IEnumerator Capture(string endpoint)
        {
            var request = new UnityWebRequest(endpoint, "POST") {
                downloadHandler = new DownloadHandlerBuffer(), uploadHandler = new UploadHandlerRaw(Array.Empty<byte>()),
                timeout = 6, redirectLimit = 0 };
            request.SetRequestHeader("Content-Type", "application/octet-stream");
            active = request; Status = "Listening on the Sense board for two seconds...";
            try
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                { Status = "Microphone unavailable. Check the updated firmware, USB bridge and connection."; yield break; }
                if (!TryReadSound(request.downloadHandler.text, out var reading))
                { Status = "Invalid microphone reading. Please retry."; yield break; }
                Status = "Sense microphone connected · listening for your sound";
                Sound?.Invoke(reading);
            }
            finally { if (ReferenceEquals(active, request)) { active = null; request.Dispose(); } }
        }
        [Serializable] private sealed class Response
        {
            public int version, samples, sample_rate, duration_ms;
            public long sequence;
            public string source;
            public float rms = -1, peak_rms = -1, clipped_fraction = -1;
        }
        public static bool TryReadSound(string json, out ToySoundReading sound)
        {
            sound = default;
            if (string.IsNullOrWhiteSpace(json) || json.Length > 1024) return false;
            var response = new Response();
            try { JsonUtility.FromJsonOverwrite(json, response); }
            catch (ArgumentException) { return false; }
            var reading = new ToySoundReading(response.rms, response.peak_rms, response.clipped_fraction);
            if (response.version != 1 || response.source != "xiao-microphone" || response.sequence < 1
                || response.samples != 32000 || response.sample_rate != 16000 || response.duration_ms != 2000 || !reading.Valid) return false;
            sound = reading; return true;
        }
    }
}
