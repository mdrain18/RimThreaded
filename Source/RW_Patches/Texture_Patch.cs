using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    internal class Texture_Patch
    {
        private static readonly MethodInfo methodget_width =
            Method(typeof(Texture), "get_width", Type.EmptyTypes);

        private static readonly Func<Texture, int> funcget_width =
            (Func<Texture, int>) Delegate.CreateDelegate(
                typeof(Func<Texture, int>), methodget_width);

        private static readonly Func<object[], object> funcget_width2 = parameters =>
            funcget_width((Texture) parameters[0]);


        private static readonly MethodInfo methodget_height =
            Method(typeof(Texture), "get_height", Type.EmptyTypes);

        private static readonly Func<Texture, int> funcget_height =
            (Func<Texture, int>) Delegate.CreateDelegate(
                typeof(Func<Texture, int>), methodget_height);

        private static readonly Func<object[], object> funcget_height2 =
            parameters => funcget_height((Texture) parameters[0]);

        internal static void RunDestructivePatches()
        {
            var original = typeof(Texture);
            var patched = typeof(Texture_Patch);
            RimThreadedHarmony.harmony.Patch(Method(original, "get_width", Type.EmptyTypes),
                new HarmonyMethod(Method(patched, nameof(get_width))));
            RimThreadedHarmony.harmony.Patch(Method(original, "get_height", Type.EmptyTypes),
                new HarmonyMethod(Method(patched, nameof(get_height))));
        }

        public static bool get_width(Texture __instance, ref int __result)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] {funcget_width2, new object[] {__instance}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (int) threadInfo.safeFunctionResult;
            return false;
        }

        public static bool get_height(Texture __instance, ref int __result)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] {funcget_height2, new object[] {__instance}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (int) threadInfo.safeFunctionResult;
            return false;
        }
    }
}