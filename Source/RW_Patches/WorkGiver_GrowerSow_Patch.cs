using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    internal class WorkGiver_GrowerSow_Patch
    {
        public static readonly Type original = typeof(WorkGiver_GrowerSow);
        public static readonly Type patched = typeof(WorkGiver_GrowerSow_Patch);

        public static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched,
                "JobOnCell"); //WorkGiver_Grower.wantedPlantDef replaced with local var for thread overwrite
        }


        public static bool JobOnCell(WorkGiver_GrowerSow __instance, ref Job __result, Pawn pawn, IntVec3 c,
            bool forced = false)
        {
            var map = pawn.Map;
            if (c.IsForbidden(pawn))
            {
                __result = null;
                return false;
            }

            if (!PlantUtility.GrowthSeasonNow(c, map, true))
            {
                __result = null;
                return false;
            }

            var localWantedPlantDef = WorkGiver_Grower.CalculateWantedPlantDef(c, map);
            WorkGiver_Grower.wantedPlantDef = localWantedPlantDef;
            if (localWantedPlantDef == null)
            {
                __result = null;
                return false;
            }

            var thingList = c.GetThingList(map);
            var flag = false;
            for (var i = 0; i < thingList.Count; i++)
            {
                var thing = thingList[i];
                if (thing.def == localWantedPlantDef)
                {
                    __result = null;
                    return false;
                }

                if ((thing is Blueprint || thing is Frame) && thing.Faction == pawn.Faction) flag = true;
            }

            if (flag)
            {
                Thing edifice = c.GetEdifice(map);
                if (edifice == null || edifice.def.fertility < 0f)
                {
                    __result = null;
                    return false;
                }
            }

            if (localWantedPlantDef.plant.cavePlant)
            {
                if (!c.Roofed(map))
                {
                    JobFailReason.Is(WorkGiver_GrowerSow.CantSowCavePlantBecauseUnroofedTrans);
                    __result = null;
                    return false;
                }

                if (map.glowGrid.GameGlowAt(c, true) > 0f)
                {
                    JobFailReason.Is(WorkGiver_GrowerSow.CantSowCavePlantBecauseOfLightTrans);
                    __result = null;
                    return false;
                }
            }

            if (localWantedPlantDef.plant.interferesWithRoof && c.Roofed(pawn.Map))
            {
                __result = null;
                return false;
            }

            var plant = c.GetPlant(map);
            if (plant != null && plant.def.plant.blockAdjacentSow)
            {
                if (!pawn.CanReserve(plant, 1, -1, null, forced) || plant.IsForbidden(pawn))
                {
                    __result = null;
                    return false;
                }

                __result = JobMaker.MakeJob(JobDefOf.CutPlant, plant);
                return false;
            }

            var thing2 = PlantUtility.AdjacentSowBlocker(localWantedPlantDef, c, map);
            if (thing2 != null)
            {
                var plant2 = thing2 as Plant;
                if (plant2 != null && pawn.CanReserve(plant2, 1, -1, null, forced) && !plant2.IsForbidden(pawn))
                {
                    var plantToGrowSettable = plant2.Position.GetPlantToGrowSettable(plant2.Map);
                    if (plantToGrowSettable == null || plantToGrowSettable.GetPlantDefToGrow() != plant2.def)
                    {
                        __result = JobMaker.MakeJob(JobDefOf.CutPlant, plant2);
                        return false;
                    }
                }

                __result = null;
                return false;
            }

            var thingdef = localWantedPlantDef;
            if (thingdef != null && thingdef.plant != null && thingdef.plant.sowMinSkill > 0 && pawn != null &&
                pawn.skills != null &&
                pawn.skills.GetSkill(SkillDefOf.Plants).Level < localWantedPlantDef.plant.sowMinSkill)
            {
                WorkGiver workGiver = __instance;
                JobFailReason.Is("UnderAllowedSkill".Translate(localWantedPlantDef.plant.sowMinSkill),
                    workGiver.def.label);
                __result = null;
                return false;
            }

            for (var j = 0; j < thingList.Count; j++)
            {
                var thing3 = thingList[j];
                if (!thing3.def.BlocksPlanting()) continue;
                if (!pawn.CanReserve(thing3, 1, -1, null, forced))
                {
                    __result = null;
                    return false;
                }

                if (thing3.def.category == ThingCategory.Plant)
                {
                    if (!thing3.IsForbidden(pawn))
                    {
                        __result = JobMaker.MakeJob(JobDefOf.CutPlant, thing3);
                        return false;
                    }

                    __result = null;
                    return false;
                }

                if (thing3.def.EverHaulable)
                {
                    __result = HaulAIUtility.HaulAsideJobFor(pawn, thing3);
                    return false;
                }

                __result = null;
                return false;
            }

            if (!localWantedPlantDef.CanEverPlantAt(c, map) || !PlantUtility.GrowthSeasonNow(c, map, true) ||
                !pawn.CanReserve(c, 1, -1, null, forced))
            {
                __result = null;
                return false;
            }

            var job = JobMaker.MakeJob(JobDefOf.Sow, c);
            job.plantDefToSow = localWantedPlantDef;
            __result = job;
            return false;
        }
    }
}