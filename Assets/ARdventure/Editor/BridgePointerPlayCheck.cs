using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ARdventure.Editor
{
    // Exercises the real screen's Update/Input System path in Editor Play mode.
    // Uses an isolated virtual device; never moves the operating-system pointer.
    public sealed class BridgePointerPlayCheck
    {
        private Mouse testMouse;
        private BalanceBridgeScreen screen;
        private int checks, lastFrame = -1;
        private float waitUntil;
        private IEnumerator routine;
        private static BridgePointerPlayCheck running;
        private InputSettings.EditorInputBehaviorInPlayMode previousInputBehavior;
        private bool changedInputBehavior;

        [MenuItem("ARdventure/Run Bridge Pointer Play Check")]
        public static void RunCheck()
        {
            var screen = UnityEngine.Object.FindFirstObjectByType<BalanceBridgeScreen>();
            if (!EditorApplication.isPlaying || screen == null)
            { Debug.LogWarning("Enter Play and open River Crossing (4) before running the pointer play check."); return; }
            if (running != null) return;
            running = new BridgePointerPlayCheck { screen = screen };
            running.routine = running.Run();
            EditorApplication.update += running.Tick;
            AssemblyReloadEvents.beforeAssemblyReload += running.Cancel;
            EditorApplication.playModeStateChanged += running.PlayModeChanged;
        }
        private void Tick()
        {
            if (!EditorApplication.isPlaying || screen == null) { Cancel(); return; }
            if (Time.frameCount == lastFrame || Time.realtimeSinceStartup < waitUntil) return;
            lastFrame = Time.frameCount;
            try
            {
                if (!routine.MoveNext()) { Cancel(); return; }
                if (routine.Current is float seconds) waitUntil = Time.realtimeSinceStartup + seconds;
            }
            catch (Exception error) { Debug.LogException(error); Cancel(); }
        }
        private void PlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Cancel();
        }
        private void Cancel()
        {
            EditorApplication.update -= Tick;
            AssemblyReloadEvents.beforeAssemblyReload -= Cancel;
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            (routine as IDisposable)?.Dispose();
            routine = null; Cleanup(); running = null;
        }

        private void Send(float forward, float deviation, bool held)
        {
            testMouse.MakeCurrent();
            var guiPoint = new Vector2(220 + forward * 840, 390 + deviation * 32);
            InputSystem.QueueStateEvent(testMouse, new MouseState {
                position = new Vector2(guiPoint.x / 1280 * screen.ViewportSize.x, (1 - guiPoint.y / 800) * screen.ViewportSize.y),
                buttons = (ushort)(held ? 1 : 0) });
        }
        private void Check(bool value, string description)
        {
            if (!value) throw new Exception("Bridge pointer play check failed: " + description
                + $"; virtual mouse {testMouse?.position.ReadValue()}, held {testMouse?.leftButton.isPressed}, viewport {screen.ViewportSize}, current mouse {Mouse.current?.name}");
            checks++;
        }
        private IEnumerator Run()
        {
            try
            {
                Check(screen.CrossingProgress == 0 && !screen.Complete, "Fresh crossing is ready");
                previousInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
                changedInputBehavior = true;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                testMouse = InputSystem.AddDevice<Mouse>("BridgeCheckMouse");
                Send(0, 0, false); yield return null; yield return null;
                Send(0, 0, true); yield return null; yield return null;
                Check(screen.Tracing, "Pointer down grabs explorer through Mouse input");
                for (int i = 1; i <= 12; i++) { Send(i / 60f, .3f, true); yield return null; }
                yield return null;
                Check(screen.CrossingProgress >= .19f, "Pointer movement maps to bridge progress");
                Send(.2f, .3f, false); yield return null; yield return null;
                float restedAt = screen.CrossingProgress;
                Send(.8f, 0, false); yield return null; yield return null;
                Check(!screen.Tracing && screen.CrossingProgress == restedAt, "Releasing and repositioning preserves progress");
                Send(restedAt, 0, true); yield return null; yield return null;
                Check(screen.Tracing, "Explorer can be grabbed again to resume");
                Send(restedAt, 2, true); yield return null;
                yield return 1f;
                Check(screen.Slips == 1 && screen.CrossingProgress == 0 && !screen.Tracing, "Sustained pointer drift returns to bank");
                Send(0, 0, false); yield return null; yield return null;
                Send(0, 0, true); yield return null; yield return null;
                for (int i = 1; i <= 60; i++) { Send(i / 60f, .2f * Mathf.Sin(i), true); yield return null; }
                yield return null;
                Check(screen.Complete && screen.CrossingProgress == 1, "Continuous pointer trace completes actual screen");
                Send(1, 0, false); yield return null;
                Debug.Log("ARdventure: all " + checks + " bridge pointer play checks passed using a virtual Mouse device.");
            }
            finally { Cleanup(); }
        }
        private void Cleanup()
        {
            if (testMouse != null && testMouse.added) InputSystem.RemoveDevice(testMouse);
            testMouse = null;
            if (changedInputBehavior) InputSystem.settings.editorInputBehaviorInPlayMode = previousInputBehavior;
            changedInputBehavior = false;
        }
    }
}
