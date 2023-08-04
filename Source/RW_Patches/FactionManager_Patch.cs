using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class FactionManager_Patch
    {
        public static List<Faction> allFactionsTickList;
        public static int allFactionsTicks;

        internal static void RunDestructivePatches()
        {
            var original = typeof(FactionManager);
            var patched = typeof(FactionManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(FactionManagerTick));
        }

        public static bool FactionManagerTick(FactionManager __instance)
        {
            SettlementProximityGoodwillUtility.CheckSettlementProximityGoodwillChange();

            lock (__instance)
            {
                var newList = __instance.toRemove;
                for (var num = newList.Count - 1; num >= 0; num--)
                {
                    var faction = newList[num];
                    newList.Remove(faction);
                    __instance.toRemove = newList;
                    __instance.Remove(faction);
                }
            }

            allFactionsTickList = __instance.allFactions;
            allFactionsTicks = allFactionsTickList.Count;
            return false;
        }

        public static void FactionsPrepare()
        {
            try
            {
                var world = Find.World;
                world.factionManager.FactionManagerTick();
            }
            catch (Exception ex3)
            {
                Log.Error(ex3.ToString());
            }
        }

        public static void FactionsListTick()
        {
            while (true)
            {
                var index = Interlocked.Decrement(ref allFactionsTicks);
                if (index < 0) return;
                var faction = allFactionsTickList[index];
                try
                {
                    faction.FactionTick();
                }
                catch (Exception ex)
                {
                    Log.Error("Exception ticking faction: " + faction.ToStringSafe() + ": " + ex);
                }
            }
        }
    }
}