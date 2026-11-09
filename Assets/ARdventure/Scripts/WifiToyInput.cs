using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ARdventure
{
    // This adapter owns transport only. Quest progression still uses IToyInput events.
    public sealed class WifiToyInput : MonoBehaviour, IToyInput
    {
        public event Action<Color> ColorScanned;
        public string Address { get; set; } = "http://127.0.0.1:8765";
        public string Status { get; private set; } = "Start the USB bridge, then aim and scan.";
        public bool Busy => activeRequest != null;
        private UnityWebRequest activeRequest;
        private bool acceptingScans = true;

        public bool AcceptingScans
        {
            get => acceptingScans;
            set { acceptingScans = value; if (!value) CancelPending(); }
        }

        public void Scan()
        {
            if (!isActiveAndEnabled || !acceptingScans || Busy) return;
            if (!TryGetEndpoint(Address, out var endpoint))
            { Status = "Enter an HTTP address, e.g. http://192.168.4.1"; return; }
            StartCoroutine(Capture(endpoint));
        }

        public void CancelPending()
        {
            if (activeRequest == null) return;
            var cancelled = activeRequest;
            activeRequest = null;
            cancelled.Abort();
            StopAllCoroutines();
            cancelled.Dispose();
            Status = "Scan cancelled. Scan again when ready.";
        }

        private void OnDisable() => CancelPending();

        private IEnumerator Capture(string endpoint)
        {
            var request = new UnityWebRequest(endpoint, "POST");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            request.SetRequestHeader("Content-Type", "application/octet-stream");
            request.timeout = 5;
            request.redirectLimit = 0;
            activeRequest = request;
            Status = "Scanning with toy camera...";
            try
            {
                yield return request.SendWebRequest();
                if (!acceptingScans) yield break;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Status = request.responseCode == 503
                        ? "Camera not ready. Wait, or check the camera connection."
                        : "Toy unavailable. Check connection and power, then retry.";
                    yield break;
                }
                if (!TryReadColour(request.downloadHandler.text, out var colour))
                { Status = "Toy returned an invalid reading. Please retry."; yield break; }
                Status = $"Camera RGB: {Mathf.RoundToInt(colour.r * 255)}, {Mathf.RoundToInt(colour.g * 255)}, {Mathf.RoundToInt(colour.b * 255)}";
                ColorScanned?.Invoke(colour);
            }
            finally
            {
                if (ReferenceEquals(activeRequest, request))
                { activeRequest = null; request.Dispose(); }
            }
        }

        public static bool TryGetEndpoint(string address, out string endpoint)
        {
            endpoint = null;
            if (!Uri.TryCreate(address?.Trim(), UriKind.Absolute, out var uri)
                || uri.Scheme != "http" || string.IsNullOrEmpty(uri.Host)
                || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
                || uri.AbsolutePath != "/") return false;
            endpoint = uri.GetLeftPart(UriPartial.Authority) + "/v1/scan";
            return true;
        }

        [Serializable]
        private sealed class ScanResponse
        {
            public int version;
            public string source;
            public float r, g, b;
            public long sequence;
            public int samples;
        }

        public static bool TryReadColour(string json, out Color colour)
        {
            colour = default;
            if (string.IsNullOrWhiteSpace(json) || json.Length > 1024) return false;
            // Missing components must fail, rather than silently becoming zero.
            var response = new ScanResponse { r = -1, g = -1, b = -1 };
            try { JsonUtility.FromJsonOverwrite(json, response); }
            catch (ArgumentException) { return false; }
            if (response.version != 1 || response.source != "xiao-camera" || response.sequence < 1
                || response.samples < 1 || response.samples > 320 * 240
                || !Channel(response.r) || !Channel(response.g) || !Channel(response.b)) return false;
            colour = new Color(response.r / 255f, response.g / 255f, response.b / 255f);
            return true;
        }

        private static bool Channel(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 255;
    }
}
