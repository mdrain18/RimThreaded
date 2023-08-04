﻿using System.Threading;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.Mod_Patches
{
    internal class TD_Enhancement_Patch
    {
        public static ReaderWriterLockSlim learnedInfo_Lock =
            new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public static void Patch()
        {
            var TD_Enhancement_Pack_Learn_Patch = TypeByName("TD_Enhancement_Pack.Learn_Patch");
            if (TD_Enhancement_Pack_Learn_Patch != null)
            {
                var methodName = "Postfix";
                Log.Message("RimThreaded is patching " + TD_Enhancement_Pack_Learn_Patch.FullName + " " + methodName);
                RimThreadedHarmony.Prefix(TD_Enhancement_Pack_Learn_Patch, typeof(TD_Enhancement_Patch), methodName,
                    destructive: false, PatchMethod: nameof(WriterPrefix), finalizer: nameof(WriterFinalizer));
            }

            var LearnedGameComponent = TypeByName("TD_Enhancement_Pack.LearnedGameComponent");
            if (LearnedGameComponent != null)
            {
                var methodName = "GameComponentTick";
                Log.Message("RimThreaded is patching " + LearnedGameComponent.FullName + " " + methodName);
                RimThreadedHarmony.Prefix(LearnedGameComponent, typeof(TD_Enhancement_Patch), methodName,
                    destructive: false, PatchMethod: nameof(WriterPrefix), finalizer: nameof(WriterFinalizer));
            }
        }

        public static void WriterPrefix()
        {
            learnedInfo_Lock.EnterWriteLock();
        }

        public static void WriterFinalizer()
        {
            learnedInfo_Lock.ExitWriteLock();
        }
    }
}