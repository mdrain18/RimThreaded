using System;
using System.Reflection;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    public class Camera_Patch
    {
        private static readonly MethodInfo MethodSetTargetTexture =
            Method(typeof(Camera), "set_targetTexture", new[] {typeof(RenderTexture)});

        private static readonly Action<Camera, RenderTexture> ActionSetTargetTexture =
            (Action<Camera, RenderTexture>) Delegate.CreateDelegate
                (typeof(Action<Camera, RenderTexture>), MethodSetTargetTexture);

        private static readonly Action<object[]> SafeActionSetTargetTexture = parameters =>
            ActionSetTargetTexture(
                (Camera) parameters[0],
                (RenderTexture) parameters[1]);

        private static readonly MethodInfo MethodRender =
            Method(typeof(Camera), "Render", new Type[] { });

        private static readonly Action<Camera> ActionRender =
            (Action<Camera>) Delegate.CreateDelegate
                (typeof(Action<Camera>), MethodRender);

        private static readonly Action<object[]> SafeActionRender = p =>
            ActionRender((Camera) p[0]);

        public static bool set_targetTexture(Camera __instance, RenderTexture value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[]
            {
                SafeActionSetTargetTexture, new object[] {__instance, value}
            };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

        public static bool Render(Camera __instance)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] {SafeActionRender, new object[] {__instance}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
    }
}