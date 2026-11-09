using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ARdventure
{
    public sealed class SimulatedMotionInput : MonoBehaviour, IToyMotionInput
    {
        public event Action<ToyMotion> Motion;
        public bool Enabled = true, KeyboardEnabled = true;
        public void Send(ToyMotion motion) { if (Enabled && isActiveAndEnabled) Motion?.Invoke(motion); }
        private void Update()
        {
            var k = Keyboard.current;
            if (!Enabled || !KeyboardEnabled || k == null) return;
            if (k.xKey.wasPressedThisFrame) Send(ToyMotion.Shake);
            if (k.zKey.wasPressedThisFrame) Send(ToyMotion.TiltLeft);
            if (k.cKey.wasPressedThisFrame) Send(ToyMotion.TiltRight);
            if (k.vKey.wasPressedThisFrame) Send(ToyMotion.Still);
        }
    }
}
