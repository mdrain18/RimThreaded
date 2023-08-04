﻿using System;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class Map_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(Map);
            var patched = typeof(Map_Patch);
            RimThreadedHarmony.Prefix(original, patched, "get_IsPlayerHome");
        }

        public static bool get_IsPlayerHome(Map __instance, ref bool __result)
        {
            if (__instance.info != null && __instance.info.parent != null && __instance.info.parent.def != null &&
                __instance.info.parent.def.canBePlayerHome)
            {
                __result = __instance.info.parent.Faction == Faction.OfPlayer;
                return false;
            }

            __result = false;
            return false;
        }

        public static void MapsPostTickPrepare()
        {
            SteadyEnvironmentEffects_Patch.totalSteadyEnvironmentEffectsTicks = 0;
            SteadyEnvironmentEffects_Patch.steadyEnvironmentEffectsTicksCompleted = 0;
            SteadyEnvironmentEffects_Patch.steadyEnvironmentEffectsCount = 0;
            WildPlantSpawner_Patch.wildPlantSpawnerCount = 0;
            WildPlantSpawner_Patch.wildPlantSpawnerTicksCount = 0;
            WildPlantSpawner_Patch.wildPlantSpawnerTicksCompleted = 0;
            TradeShip_Patch.totalTradeShipTicks = 0;
            TradeShip_Patch.totalTradeShipTicksCompleted = 0;
            TradeShip_Patch.totalTradeShipsCount = 0;
            try
            {
                var maps = Find.Maps;
                for (var j = 0; j < maps.Count; j++)
                {
                    var map = maps[j];
                    map.MapPostTick();
                }
            }
            catch (Exception ex3)
            {
                Log.Error(ex3.ToString());
            }
        }

        public static void MapPostListTick()
        {
            SteadyEnvironmentEffects_Patch.SteadyEffectTick();
            WildPlantSpawner_Patch.WildPlantSpawnerListTick();
            TradeShip_Patch.PassingShipListTick();
        }
    }
}