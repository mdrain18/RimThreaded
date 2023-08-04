using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    internal class GraphicsFormatUtility_Patch
    {
        private static readonly Type original = typeof(GraphicsFormatUtility);
        private static readonly Type patched = typeof(GraphicsFormatUtility_Patch);

        private static readonly MethodInfo methodGetGraphicsFormat =
            Method(original, "GetGraphicsFormat", new[] {typeof(RenderTextureFormat), typeof(RenderTextureReadWrite)});

        private static readonly Func<RenderTextureFormat, RenderTextureReadWrite, GraphicsFormat>
            funcGetGraphicsFormat =
                (Func<RenderTextureFormat, RenderTextureReadWrite, GraphicsFormat>) Delegate.CreateDelegate(
                    typeof(Func<RenderTextureFormat, RenderTextureReadWrite, GraphicsFormat>), methodGetGraphicsFormat);

        private static readonly Func<object[], object> funcGetGraphicsFormat2 = parameters =>
            funcGetGraphicsFormat((RenderTextureFormat) parameters[0], (RenderTextureReadWrite) parameters[1]);

        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.harmony.Patch(methodGetGraphicsFormat,
                new HarmonyMethod(Method(patched, nameof(GetGraphicsFormat))));
        }

        public static bool GetGraphicsFormat(ref GraphicsFormat __result, RenderTextureFormat format,
            RenderTextureReadWrite readWrite)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] {funcGetGraphicsFormat2, new object[] {format, readWrite}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (GraphicsFormat) threadInfo.safeFunctionResult;
            return false;
        }
    }
}