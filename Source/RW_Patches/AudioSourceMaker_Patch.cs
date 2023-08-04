using System;
using UnityEngine;
using Verse.Sound;
using static System.Threading.Thread;
using static RimThreaded.RimThreaded;

namespace RimThreaded.RW_Patches
{
    public class AudioSourceMaker_Patch
    {
        private static readonly Func<object[], object> safeFunction = parameters =>
            AudioSourceMaker.NewAudioSourceOn((GameObject) parameters[0]);

        public static void RunDestructivePatches()
        {
            var original = typeof(AudioSourceMaker);
            var patched = typeof(AudioSourceMaker_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(NewAudioSourceOn));
        }

        public static bool NewAudioSourceOn(ref AudioSource __result, GameObject go)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] {safeFunction, new object[] {go}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (AudioSource) threadInfo.safeFunctionResult;
            return false;
        }
    }
}