using System;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    public class MeshMakerShadows_Patch
    {
        private static readonly Func<object[], object> FuncNewShadowMesh = parameters =>
            MeshMakerShadows.NewShadowMesh(
                (float) parameters[0],
                (float) parameters[1],
                (float) parameters[2]);

        internal static void RunDestructivePatches()
        {
            var original = typeof(MeshMakerShadows);
            var patched = typeof(MeshMakerShadows_Patch);
            RimThreadedHarmony.Prefix(original, patched, "NewShadowMesh",
                new[] {typeof(float), typeof(float), typeof(float)});
        }

        public static bool NewShadowMesh(ref Mesh __result, float baseWidth, float baseHeight, float tallness)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[]
                {FuncNewShadowMesh, new object[] {baseWidth, baseHeight, tallness}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (Mesh) threadInfo.safeFunctionResult;
            return false;
        }
    }
}