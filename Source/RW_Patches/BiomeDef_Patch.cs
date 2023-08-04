using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class BiomeDef_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(BiomeDef);
            var patched = typeof(BiomeDef_Patch);
            RimThreadedHarmony.Prefix(original, patched, "CachePlantCommonalitiesIfShould");
        }

        public static bool CachePlantCommonalitiesIfShould(BiomeDef __instance)
        {
            if (__instance.cachedPlantCommonalities != null) return false;
            if (__instance.cachedPlantCommonalities != null) return false;
            lock (__instance) //TODO more efficient lock
            {
                var localCachedPlantCommonalities = new Dictionary<ThingDef, float>();
                for (var i = 0; i < __instance.wildPlants.Count; i++)
                {
                    var wildPlant = __instance.wildPlants[i];
                    var plant = wildPlant.plant;
                    if (plant != null) localCachedPlantCommonalities[plant] = wildPlant.commonality;
                }

                foreach (var allDef in DefDatabase<ThingDef>.AllDefs)
                {
                    if (allDef.plant == null || allDef.plant.wildBiomes == null) continue;
                    for (var j = 0; j < allDef.plant.wildBiomes.Count; j++)
                        if (allDef.plant.wildBiomes[j].biome == __instance)
                            localCachedPlantCommonalities.Add(allDef, allDef.plant.wildBiomes[j].commonality);
                }

                __instance.cachedPlantCommonalitiesSum = localCachedPlantCommonalities.Sum(x => x.Value);
                __instance.cachedPlantCommonalities = localCachedPlantCommonalities;
            }

            return false;
        }
    }
}