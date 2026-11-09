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
        public bool KeyboardEnabled { get; set; } = true;

        private void Update()
        {
            if (!Enabled || !KeyboardEnabled || Keyboard.current == null) return;
            if (Keyboard.current.gKey.wasPressedThisFrame) ScanGreen();
            if (Keyboard.current.bKey.wasPressedThisFrame) ScanBlue();
            if (Keyboard.current.yKey.wasPressedThisFrame) ScanGold();
            if (Keyboard.current.nKey.wasPressedThisFrame) ScanDark();
            if (Keyboard.current.lKey.wasPressedThisFrame) ScanLight();
        }

        public void ScanGreen() { if (Enabled) ColorScanned?.Invoke(new Color(0.12f, 0.8f, 0.25f)); }
        public void ScanBlue() { if (Enabled) ColorScanned?.Invoke(new Color(0.12f, 0.25f, 0.8f)); }
        public void ScanGold() { if (Enabled) ColorScanned?.Invoke(new Color(0.95f, 0.75f, 0.1f)); }
        public void ScanDark() { if (Enabled) ColorScanned?.Invoke(new Color(0.04f, 0.04f, 0.04f)); }
        public void ScanLight() { if (Enabled) ColorScanned?.Invoke(new Color(0.8f, 0.8f, 0.8f)); }
    }
}
