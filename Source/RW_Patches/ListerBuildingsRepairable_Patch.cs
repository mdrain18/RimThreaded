using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class ListerBuildingsRepairable_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(ListerBuildingsRepairable);
            var patched = typeof(ListerBuildingsRepairable_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(UpdateBuilding));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_BuildingDeSpawned));
        }

        public static bool Notify_BuildingDeSpawned(ListerBuildingsRepairable __instance, Building b)
        {
            if (b.Faction != null)
                lock (__instance)
                {
                    __instance.ListFor(b.Faction).Remove(b);
                    __instance.HashSetFor(b.Faction).Remove(b);
                }

            return false;
        }

        public static bool UpdateBuilding(ListerBuildingsRepairable __instance, Building b)
        {
            var faction = b.Faction;
            if (faction == null || !b.def.building.repairable)
                return false;
            lock (__instance)
            {
                var thingList = __instance.ListFor(faction);
                var thingSet = __instance.HashSetFor(faction);
                if (b.HitPoints < b.MaxHitPoints)
                {
                    if (!thingList.Contains(b))
                        thingList.Add(b);
                    thingSet.Add(b);
                }
                else
                {
                    var newthingList = new List<Thing>(thingList);
                    newthingList.Remove(b);
                    __instance.repairables[faction] = newthingList;
                    var newthingSet = new HashSet<Thing>(thingSet);
                    newthingSet.Remove(b);
                    __instance.repairablesSet[faction] = newthingSet;
                }
            }

            return false;
        }
    }
}