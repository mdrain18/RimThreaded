using System;
using static System.Threading.Thread;
using static RimThreaded.RimThreaded;
using Object = UnityEngine.Object;

namespace RimThreaded.RW_Patches
{
    internal class UnityEngine_Object_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(Object);
            var patched = typeof(UnityEngine_Object_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(ToString), new Type[] { });
            RimThreadedHarmony.Prefix(original, patched, nameof(Destroy), new[] {typeof(Object)});
        }

        public static bool ToString(Object __instance, ref string __result)
        {
            if (!CurrentThread.IsBackground ||
                !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo)) return true;
            Func<object[], object> safeFunction = parameters => __instance.ToString();
            threadInfo.safeFunctionRequest = new object[] {safeFunction, new object[] { }};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (string) threadInfo.safeFunctionResult;
            return false;
        }

        public static bool Destroy(Object __instance)
        {
            if (!CurrentThread.IsBackground ||
                !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo)) return true;
            Action<object[]> safeActionDestroy = parameters => Object.Destroy(__instance);
            threadInfo.safeFunctionRequest = new object[] {safeActionDestroy, new object[] { }};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
    }
}