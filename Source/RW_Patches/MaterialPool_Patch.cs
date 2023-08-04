using System;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    public class MaterialPool_Patch
    {
        private static readonly Func<object[], object> FuncMatFrom = parameters =>
            MaterialPool.MatFrom((MaterialRequest) parameters[0]);

        public static void RunDestructivePatches()
        {
            var original = typeof(MaterialPool);
            var patched = typeof(MaterialPool_Patch);
            RimThreadedHarmony.Prefix(original, patched, "MatFrom", new[] {typeof(MaterialRequest)});
        }

        public static bool MatFrom(ref Material __result, MaterialRequest req)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] {FuncMatFrom, new object[] {req}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (Material) threadInfo.safeFunctionResult;
            return false;
        }
    }
}