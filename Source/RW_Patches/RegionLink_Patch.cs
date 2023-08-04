﻿using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class RegionLink_Patch
    {
        private static readonly Type original = typeof(RegionLink);
        private static readonly Type patched = typeof(RegionLink_Patch);

        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, nameof(Register));
        }

        public static bool Register(RegionLink __instance, Region reg)
        {
            var regionA = __instance.RegionA;
            var regionB = __instance.RegionB;
            if (__instance.regions[0] == reg || __instance.regions[1] == reg)
                Log.Error("Tried to double-register region " + reg + " in " + __instance);
            else if (regionA == null || !regionA.valid)
                __instance.RegionA = reg;
            else if (regionB == null || !regionB.valid)
                __instance.RegionB = reg;
            else
                Log.Warning("Could not register region " + reg + " in link " + __instance +
                            ": > 2 regions on link!\nRegionA: " + __instance.RegionA.DebugString + "\nRegionB: " +
                            __instance.RegionB.DebugString);

            //TODO find root cause
            //RegionAndRoomUpdater_Patch.regionsToReDirty.Add(regionA);
            //RegionAndRoomUpdater_Patch.regionsToReDirty.Add(regionB);
            //RegionAndRoomUpdater_Patch.regionsToReDirty.Add(reg);
            //RegionDirtyer_Patch.SetRegionDirty(reg.Map.regionDirtyer, regionA);
            //RegionDirtyer_Patch.SetRegionDirty(reg.Map.regionDirtyer, regionB);
            /*
                foreach (IntVec3 cell in reg.Cells)
                {
                    if (regionA.Cells.Contains(cell))
                    {
                        __instance.RegionA = reg;
                        RegionDirtyer_Patch.SetRegionDirty(reg.Map.regionDirtyer, regionA);
                        break;
                    }
                    if (regionB.Cells.Contains(cell))
                    {
                        __instance.RegionB = reg;
                        RegionDirtyer_Patch.SetRegionDirty(reg.Map.regionDirtyer, regionB);
                        break;
                    }
                }
                */

            return false;
        }
    }
}