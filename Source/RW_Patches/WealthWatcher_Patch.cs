using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class WealthWatcher_Patch
    {
        private static readonly Type original = typeof(WealthWatcher);
        private static readonly Type patched = typeof(WealthWatcher_Patch);

        public static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "ResetStaticData");
        }

        public static bool ResetStaticData(WealthWatcher __instance)
        {
            var num = -1;
            var allDefsListForReading = DefDatabase<TerrainDef>.AllDefsListForReading;
            for (var i = 0; i < allDefsListForReading.Count; i++) num = Mathf.Max(num, allDefsListForReading[i].index);

            var newCachedTerrainMarketValue = new float[num + 1];
            for (var j = 0; j < allDefsListForReading.Count; j++)
                newCachedTerrainMarketValue[allDefsListForReading[j].index] =
                    allDefsListForReading[j].GetStatValueAbstract(StatDefOf.MarketValue);
            WealthWatcher.cachedTerrainMarketValue = newCachedTerrainMarketValue;
            return false;
        }
    }
}