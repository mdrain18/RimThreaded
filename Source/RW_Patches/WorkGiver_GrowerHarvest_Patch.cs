using RimWorld;
using Verse;
using Verse.AI;
using static RimThreaded.JumboCell;

namespace RimThreaded.RW_Patches
{
    internal class WorkGiver_GrowerHarvest_Patch
    {
        public static IntVec3 ClosestLocationReachable(WorkGiver_GrowerHarvest workGiver_GrowerHarvest, Pawn pawn)
        {
            var maxDanger = pawn.NormalMaxDanger();
            var map = pawn.Map;
            var zoneManager = pawn.Map.zoneManager;
            foreach (var actionableLocation in GetClosestActionableLocations(pawn, map, RimThreaded.plantHarvest_Cache))
            {
                var thingsAtLocation = actionableLocation.GetThingList(map);
                foreach (var thingAtLocation in thingsAtLocation)
                    if (thingAtLocation is Building_PlantGrower building_PlantGrower)
                    {
                        if (building_PlantGrower == null || !workGiver_GrowerHarvest.ExtraRequirements(
                                                             building_PlantGrower, pawn)
                                                         || building_PlantGrower.IsForbidden(pawn)
                                                         || !pawn.CanReach(building_PlantGrower, PathEndMode.OnCell,
                                                             maxDanger)
                           )
                            continue;
                        var plant = actionableLocation.GetPlant(pawn.Map);
                        var hasJobOnCell = plant != null && plant.HarvestableNow &&
                                           plant.LifeStage == PlantLifeStage.Mature && plant.CanYieldNow();
                        if (!hasJobOnCell) break;
                        var canReserve = ReservationManager_Patch.IsUnreserved(map.reservationManager, plant);
                        if (!canReserve)
                            break;
                        return actionableLocation;
                    }

                if (!(zoneManager.ZoneAt(actionableLocation) is Zone_Growing growZone)) continue;
                if (!workGiver_GrowerHarvest.ExtraRequirements(growZone, pawn)) continue;
                if (!workGiver_GrowerHarvest.HasJobOnCell(pawn, actionableLocation))
                {
                    ReregisterObject(pawn.Map, actionableLocation, RimThreaded.plantHarvest_Cache);
                    continue;
                }

                if (!pawn.CanReach(actionableLocation, PathEndMode.OnCell, maxDanger)) continue;
                return actionableLocation;
            }

            return IntVec3.Invalid;
        }
    }
}