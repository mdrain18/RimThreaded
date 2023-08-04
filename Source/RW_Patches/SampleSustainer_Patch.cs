﻿using System;
using UnityEngine;
using Verse.Sound;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    public class SampleSustainer_Patch
    {
        private static readonly Func<object[], object> safeFunction = parameters =>
            SampleSustainer.TryMakeAndPlay(
                (SubSustainer) parameters[0],
                (AudioClip) parameters[1],
                (float) parameters[2]);

        public static void RunDestructivePatches()
        {
            var original = typeof(SampleSustainer);
            var patched = typeof(SampleSustainer_Patch);
            RimThreadedHarmony.Prefix(original, patched, "TryMakeAndPlay");
        }

        public static bool TryMakeAndPlay(ref SampleSustainer __result, SubSustainer subSus, AudioClip clip,
            float scheduledEndTime)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] {safeFunction, new object[] {subSus, clip, scheduledEndTime}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (SampleSustainer) threadInfo.safeFunctionResult;
            return false;
        }
    }
}