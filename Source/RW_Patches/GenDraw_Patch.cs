using System;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    internal class GenDraw_Patch
    {
        private static readonly Action<object[]> ActionDrawMeshNowOrLater = p =>
            GenDraw.DrawMeshNowOrLater((Mesh) p[0], (Vector3) p[1], (Quaternion) p[2], (Material) p[3], (bool) p[4]);

        internal static void RunDestructivePatches()
        {
            var original = typeof(GenDraw);
            var patched = typeof(GenDraw_Patch);


            RimThreadedHarmony.Prefix(original, patched, "DrawMeshNowOrLater",
                new[] {typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool)});
        }


        public static bool DrawMeshNowOrLater(Mesh mesh, Vector3 loc, Quaternion quat, Material mat, bool drawNow)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;

            threadInfo.safeFunctionRequest = new object[]
                {ActionDrawMeshNowOrLater, new object[] {mesh, loc, quat, mat, drawNow}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
    }
}