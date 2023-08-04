﻿using System;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    public class ContentFinder_Texture2D_Patch
    {
        private static readonly Func<object[], object> FuncContentFinder = parameters =>
            ContentFinder<Texture2D>.Get(
                (string) parameters[0],
                (bool) parameters[1]);

        public static void RunDestructivePatches()
        {
            var original = typeof(ContentFinder<Texture2D>);
            var patched = typeof(ContentFinder_Texture2D_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Get");
        }

        public static bool Get(ref Texture2D __result, string itemPath, bool reportFailure = true)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] {FuncContentFinder, new object[] {itemPath, reportFailure}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (Texture2D) threadInfo.safeFunctionResult;
            return false;
        }
    }
}