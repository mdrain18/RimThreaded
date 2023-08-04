using System.Collections.Generic;
using RimWorld;

namespace RimThreaded.RW_Patches
{
    internal class GoodwillSituationManager_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(GoodwillSituationManager);
            var patched = typeof(GoodwillSituationManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(Recalculate), new[] {typeof(Faction), typeof(bool)});
        }

        public static bool Recalculate(GoodwillSituationManager __instance, Faction other,
            bool canSendHostilityChangedLetter)
        {
            List<GoodwillSituationManager.CachedSituation> outSituations1;
            if (__instance.cachedData.TryGetValue(other, out outSituations1))
            {
                __instance.Recalculate(other, outSituations1);
            }
            else
            {
                var outSituations2 = new List<GoodwillSituationManager.CachedSituation>();
                __instance.Recalculate(other, outSituations2);
                lock (__instance.cachedData)
                {
                    __instance.cachedData.Add(other, outSituations2);
                }
            }

            __instance.CheckHostilityChanged(other, canSendHostilityChangedLetter);
            return false;
        }
    }
}