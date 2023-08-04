using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class RegionMaker_Patch
    {
        private static readonly Type Original = typeof(RegionMaker);
        private static readonly Type Patched = typeof(RegionMaker_Patch);

        public static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(Original, Patched, "FloodFillAndAddCells");
            RimThreadedHarmony.Prefix(Original, Patched, "CreateLinks");
        }

        public static bool FloodFillAndAddCells(RegionMaker __instance, IntVec3 root)
        {
            var newReg = __instance.newReg;
            var map = __instance.map;
            __instance.newRegCells = new List<IntVec3>();
            if (newReg.type.IsOneCellRegion())
            {
                if (!RegionAndRoomUpdater_Patch.cellsWithNewRegions.Contains(root))
                {
                    RegionAndRoomUpdater_Patch.cellsWithNewRegions.Add(root);
                    __instance.AddCell(root);
                }

                return false;
            }

            map.floodFiller.FloodFill(root,
                x => newReg.extentsLimit.Contains(x) && x.GetExpectedRegionType(map) == newReg.type, delegate(IntVec3 x)
                {
                    if (!RegionAndRoomUpdater_Patch.cellsWithNewRegions.Contains(x))
                    {
                        RegionAndRoomUpdater_Patch.cellsWithNewRegions.Add(x);
                        __instance.AddCell(x);
                    }
                });
            return false;
        }

        public static bool CreateLinks(RegionMaker __instance)
        {
            var linksProcessedAt = __instance.linksProcessedAt;
            var newRegCells = __instance.newRegCells;
            for (var i = 0; i < linksProcessedAt.Length; i++) linksProcessedAt[i] = new HashSet<IntVec3>();

            for (var j = 0; j < newRegCells.Count; j++)
            {
                var c = newRegCells[j];
                __instance.SweepInTwoDirectionsAndTryToCreateLink(Rot4.North, c);
                __instance.SweepInTwoDirectionsAndTryToCreateLink(Rot4.South, c);
                __instance.SweepInTwoDirectionsAndTryToCreateLink(Rot4.East, c);
                __instance.SweepInTwoDirectionsAndTryToCreateLink(Rot4.West, c);
            }

            return false;
        }
    }
}