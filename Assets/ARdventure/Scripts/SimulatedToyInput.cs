using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ARdventure
{
    public interface IToyInput
    {
        event Action<Color> ColorScanned;
    }

    // Explicit debug inputs remain available alongside the physical-camera adapter.
    public sealed class SimulatedToyInput : MonoBehaviour, IToyInput
    {
        public event Action<Color> ColorScanned;
        public bool Enabled { get; set; } = true;

        private void Update()
        {
            if (!Enabled || Keyboard.current == null) return;
            if (Keyboard.current.gKey.wasPressedThisFrame) ScanGreen();
            if (Keyboard.current.bKey.wasPressedThisFrame) ScanBlue();
        }

        public void ScanGreen() { if (Enabled) ColorScanned?.Invoke(new Color(0.12f, 0.8f, 0.25f)); }
        public void ScanBlue() { if (Enabled) ColorScanned?.Invoke(new Color(0.12f, 0.25f, 0.8f)); }
    }
}
