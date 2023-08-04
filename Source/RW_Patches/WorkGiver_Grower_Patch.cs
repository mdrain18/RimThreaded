﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    internal class WorkGiver_Grower_Patch
    {
        [ThreadStatic] public static ThingDef wantedPlantDef;

        public static Dictionary<Map, List<HashSet<IntVec3>[]>> awaitingPlantCellsMapDict =
            new Dictionary<Map, List<HashSet<IntVec3>[]>>();

        public static bool PotentialWorkCellsGlobal(WorkGiver_Grower __instance, ref IEnumerable<IntVec3> __result,
            Pawn pawn)
        {
            __result = PotentialWorkCellsGlobalIE(__instance, pawn);
            return false;
        }

        private static IEnumerable<IntVec3> PotentialWorkCellsGlobalIE(WorkGiver_Grower __instance, Pawn pawn)
        {
            var maxDanger = pawn.NormalMaxDanger();
            var bList = pawn.Map.listerBuildings.allBuildingsColonist;
            for (var j = 0; j < bList.Count; j++)
            {
                var building_PlantGrower = bList[j] as Building_PlantGrower;
                if (building_PlantGrower == null || !__instance.ExtraRequirements(building_PlantGrower, pawn) ||
                    building_PlantGrower.IsForbidden(pawn) ||
                    //!pawn.CanReach(building_PlantGrower, PathEndMode.OnCell, maxDanger) || 
                    building_PlantGrower.IsBurning())
                    continue;
                foreach (var item in building_PlantGrower.OccupiedRect()) yield return item;
                wantedPlantDef = null;
            }

            wantedPlantDef = null;
            var zonesList = pawn.Map.zoneManager.AllZones;
            for (var j = 0; j < zonesList.Count; j++)
            {
                var growZone = zonesList[j] as Zone_Growing;
                if (growZone == null) continue;
                if (growZone.cells.Count == 0)
                {
                    Log.ErrorOnce("Grow zone has 0 cells: " + growZone, -563487);
                }
                else if (__instance.ExtraRequirements(growZone, pawn) && !growZone.ContainsStaticFire &&
                         pawn.CanReach(growZone.Cells.First(), PathEndMode.OnCell, maxDanger))
                {
                    for (var k = 0; k < growZone.cells.Count; k++) yield return growZone.cells[k];
                    wantedPlantDef = null;
                    growZone = null;
                }
            }

            wantedPlantDef = null;
        }

        public static IEnumerable<IntVec3> PotentialWorkCellsGlobalWithoutCanReach(WorkGiver_Grower __instance,
            Pawn pawn)
        {
            var maxDanger = pawn.NormalMaxDanger();
            var bList = pawn.Map.listerBuildings.allBuildingsColonist;
            for (var j = 0; j < bList.Count; j++)
            {
                var building_PlantGrower = bList[j] as Building_PlantGrower;
                if (building_PlantGrower == null || !__instance.ExtraRequirements(building_PlantGrower, pawn) ||
                    building_PlantGrower.IsForbidden(pawn) || building_PlantGrower.IsBurning()) continue;
                foreach (var item in building_PlantGrower.OccupiedRect()) yield return item;
            }

            var zonesList = pawn.Map.zoneManager.AllZones;
            for (var j = 0; j < zonesList.Count; j++)
            {
                var growZone = zonesList[j] as Zone_Growing;
                if (growZone == null) continue;
                if (growZone.cells.Count == 0)
                    Log.ErrorOnce("Grow zone has 0 cells: " + growZone, -563487);
                else if (__instance.ExtraRequirements(growZone, pawn) && !growZone.ContainsStaticFire)
                    for (var k = 0; k < growZone.cells.Count; k++)
                        yield return growZone.cells[k];
            }
        }


        internal static IntVec3 ClosestLocationReachable(WorkGiver_Grower workGiver_Grower, Pawn pawn)
        {
            var maxDanger = pawn.NormalMaxDanger();
            //wantedPlantDef = null;
            //List<Zone> zonesList = pawn.Map.zoneManager.AllZones;
            //for (int j = 0; j < zonesList.Count; j++)
            //{

            //if (growZone.cells.Count == 0)
            //{
            //Log.ErrorOnce("Grow zone has 0 cells: " + growZone, -563487);
            //}
            var forced = false;
            var map = pawn.Map;
            var zoneManager = pawn.Map.zoneManager;
            foreach (var actionableLocation in JumboCell.GetClosestActionableLocations(pawn, map,
                         RimThreaded.plantSowing_Cache))
            {
                var thingsAtLocation = actionableLocation.GetThingList(map);
                foreach (var thingAtLocation in thingsAtLocation)
                    if (thingAtLocation is Building_PlantGrower building_PlantGrower)
                    {
                        if (building_PlantGrower == null || !workGiver_Grower.ExtraRequirements(building_PlantGrower,
                                                             pawn)
                                                         || building_PlantGrower.IsForbidden(pawn)
                                                         || !pawn.CanReach(building_PlantGrower, PathEndMode.OnCell,
                                                             maxDanger)
                            //|| building_PlantGrower.IsBurning()
                           )
                            continue;

                        //foreach (IntVec3 item in building_PlantGrower.OccupiedRect())
                        //{
                        //return item; //TODO ADD check
                        //}
                        return actionableLocation;
                    }

                if (!(zoneManager.ZoneAt(actionableLocation) is Zone_Growing growZone)) continue;
                if (!workGiver_Grower.ExtraRequirements(growZone, pawn)) continue;
                if (!JobOnCellTest(workGiver_Grower.def, pawn, actionableLocation, forced)) continue;
                //!growZone.ContainsStaticFire && 
                if (!workGiver_Grower.HasJobOnCell(pawn, actionableLocation)) continue;
                if (!pawn.CanReach(actionableLocation, PathEndMode.OnCell, maxDanger)) continue;
                return actionableLocation;
            }

            //wantedPlantDef = null;
            return IntVec3.Invalid;
        }

        private static bool JobOnCellTest(WorkGiverDef def, Pawn pawn, IntVec3 c, bool forced = false)
        {
            var map = pawn.Map;
            if (c.IsForbidden(pawn))
            {
#if DEBUG
                Log.Warning("IsForbidden");
#endif
                JumboCell.ReregisterObject(map, c, RimThreaded.plantSowing_Cache);
                return false;
            }

            if (!PlantUtility.GrowthSeasonNow(c, map, true))
            {
#if DEBUG
                Log.Warning("GrowthSeasonNow");
#endif
                return false;
            }

            var localWantedPlantDef = WorkGiver_Grower.CalculateWantedPlantDef(c, map);
            WorkGiver_Grower.wantedPlantDef = localWantedPlantDef;
            if (localWantedPlantDef == null)
            {
#if DEBUG
                Log.Warning("localWantedPlantDef==null");
#endif
                return false;
            }

            Plant plant = null;
            var thingList = c.GetThingList(map);
            var flag = false;
            for (var i = 0; i < thingList.Count; i++)
            {
                var thing = thingList[i];
                if (thing.def.category == ThingCategory.Plant)
                {
                    plant = (Plant) thing;
                    if (thing.def == localWantedPlantDef)
                    {
#if DEBUG
                        Log.Warning("thing.def == localWantedPlantDef... (plant thing needs to be removed before sowing) workgiverGrowerSowingHashset at: " + c.ToString());
#endif
                        JumboCell.ReregisterObject(map, c, RimThreaded.plantSowing_Cache);
                        //JumboCellCache.AddObjectToActionableObjects(map, c, c, awaitingPlantCellsMapDict);
                        return false;
                    }
                }


                if ((thing is Blueprint || thing is Frame) && thing.Faction == pawn.Faction) flag = true;
            }

            if (flag)
            {
                Thing edifice = c.GetEdifice(map);
                if (edifice == null || edifice.def.fertility < 0f)
                {
#if DEBUG
                    Log.Warning("fertility");
#endif
                    return false;
                }
            }

            if (localWantedPlantDef.plant.cavePlant)
            {
                if (!c.Roofed(map))
                {
#if DEBUG
                    Log.Warning("cavePlant");
#endif
                    return false;
                }

                if (map.glowGrid.GameGlowAt(c, true) > 0f)
                {
#if DEBUG
                    Log.Warning("GameGlowAt");
#endif
                    return false;
                }
            }

            if (localWantedPlantDef.plant.interferesWithRoof && c.Roofed(pawn.Map)) return false;

            //Plant plant = c.GetPlant(map);
            if (plant != null && plant.def.plant.blockAdjacentSow)
            {
                if (!pawn.CanReserve(plant, 1, -1, null, forced) || plant.IsForbidden(pawn))
                {
#if DEBUG
                    Log.Warning("blockAdjacentSow");
#endif
                    return false;
                }

                return true; // JobMaker.MakeJob(JobDefOf.CutPlant, plant);
            }

            var thing2 = PlantUtility.AdjacentSowBlocker(localWantedPlantDef, c, map);
            if (thing2 != null)
            {
                var plant2 = thing2 as Plant;
                if (plant2 != null && pawn.CanReserve(plant2, 1, -1, null, forced) && !plant2.IsForbidden(pawn))
                {
                    var plantToGrowSettable = plant2.Position.GetPlantToGrowSettable(plant2.Map);
                    if (plantToGrowSettable == null || plantToGrowSettable.GetPlantDefToGrow() != plant2.def)
                        return true; // JobMaker.MakeJob(JobDefOf.CutPlant, plant2);
                }
#if DEBUG
                Log.Warning("AdjacentSowBlocker");
#endif
                JumboCell.ReregisterObject(map, c, RimThreaded.plantSowing_Cache);
                return false;
            }

            if (localWantedPlantDef.plant.sowMinSkill > 0 && pawn.skills != null &&
                pawn.skills.GetSkill(SkillDefOf.Plants).Level < localWantedPlantDef.plant.sowMinSkill)
            {
#if DEBUG
                Log.Warning("UnderAllowedSkill");
#endif
                return false;
            }

            for (var j = 0; j < thingList.Count; j++)
            {
                var thing3 = thingList[j];
                if (!thing3.def.BlocksPlanting()) continue;

                if (!pawn.CanReserve(thing3, 1, -1, null, forced))
                {
#if DEBUG
                    Log.Warning("!CanReserve");
#endif
                    JumboCell.ReregisterObject(map, c, RimThreaded.plantSowing_Cache);

                    return false;
                }

                if (thing3.def.category == ThingCategory.Plant)
                {
                    if (!thing3.IsForbidden(pawn)) return true; // JobMaker.MakeJob(JobDefOf.CutPlant, thing3);
#if DEBUG
                    Log.Warning("Plant IsForbidden");
#endif
                    JumboCell.ReregisterObject(map, c, RimThreaded.plantSowing_Cache);

                    return false;
                }

                if (thing3.def.EverHaulable) return true; //HaulAIUtility.HaulAsideJobFor(pawn, thing3);
#if DEBUG
                Log.Warning("EverHaulable");
#endif
                JumboCell.ReregisterObject(map, c, RimThreaded.plantSowing_Cache);
                return false;
            }

            if (!localWantedPlantDef.CanEverPlantAt(c, map))
            {
#if DEBUG
                Log.Warning("CanEverPlantAt_NewTemp");
#endif
                JumboCell.ReregisterObject(map, c, RimThreaded.plantSowing_Cache);
                return false;
            }

            if (!PlantUtility.GrowthSeasonNow(c, map, true))
            {
#if DEBUG
                Log.Warning("GrowthSeasonNow");
#endif
                return false;
            }

            if (!pawn.CanReserve(c, 1, -1, null, forced))
            {
#if DEBUG
                Log.Warning("!pawn.CanReserve(c)");
#endif
                JumboCell.ReregisterObject(map, c, RimThreaded.plantSowing_Cache);
                //JumboCellCache.AddObjectToActionableObjects(map, c, c, awaitingPlantCellsMapDict);
                return false;
            }

            //Job job = JobMaker.MakeJob(JobDefOf.Sow, c);
            //job.plantDefToSow = wantedPlantDef;
            return true; //job;
        }
    }
}