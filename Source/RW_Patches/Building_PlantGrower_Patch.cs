﻿using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class Building_PlantGrower_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            var original = typeof(Building_PlantGrower);
            var patched = typeof(Building_PlantGrower_Patch);
            RimThreadedHarmony.Postfix(original, patched, nameof(SetPlantDefToGrow));
        }

        public static void SetPlantDefToGrow(Building_PlantGrower __instance, ThingDef plantDef)
        {
            if (Current.ProgramState == ProgramState.Playing)
                foreach (var c in __instance.OccupiedRect())
                    JumboCell.ReregisterObject(__instance.Map, c, RimThreaded.plantSowing_Cache);
        }
    }
}