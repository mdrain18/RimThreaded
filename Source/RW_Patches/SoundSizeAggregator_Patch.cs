using System.Collections.Generic;
using Verse.Sound;

namespace RimThreaded.RW_Patches
{
    public class SoundSizeAggregator_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(SoundSizeAggregator);
            var patched = typeof(SoundSizeAggregator_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(RegisterReporter));
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveReporter));
            RimThreadedHarmony.Prefix(original, patched, nameof(get_AggregateSize));
        }

        public static bool RegisterReporter(SoundSizeAggregator __instance, ISizeReporter newRep)
        {
            lock (__instance.reporters) //added lock
            {
                __instance.reporters.Add(newRep);
            }

            return false;
        }

        public static bool RemoveReporter(SoundSizeAggregator __instance, ISizeReporter oldRep)
        {
            lock (__instance.reporters)
            {
                var newReporters = new List<ISizeReporter>(__instance.reporters); //safe copy remove
                newReporters.Remove(oldRep);
                __instance.reporters = newReporters;
            }

            return false;
        }

        public static bool get_AggregateSize(SoundSizeAggregator __instance, ref float __result)
        {
            if (__instance.reporters.Count == 0)
            {
                __result = __instance.testSize;
                return false;
            }

            var num = 0f;
            foreach (var reporter in __instance.reporters)
                if (reporter != null) // added check for null
                    num += reporter.CurrentSize();
            __result = num;
            return false;
        }
    }
}