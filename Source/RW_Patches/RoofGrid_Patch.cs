﻿using Verse;

namespace RimThreaded.RW_Patches
{
    internal class RoofGrid_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(RoofGrid);
            var patched = typeof(RoofGrid_Patch);

            RimThreadedHarmony.Prefix(original, patched, nameof(SetRoof));
        }

        public static bool SetRoof(RoofGrid __instance, IntVec3 c, RoofDef def)
        {
            var map = __instance.map;
            var mcc = map.cellIndices.CellToIndex(c);
            if (__instance.roofGrid[mcc] != def)
            {
                __instance.roofGrid[mcc] = def;
                map.glowGrid.MarkGlowGridDirty(c);
                //Comment the 3 following lines and uncomment the 4th to fix the roof notification -Sernior
                var room = map.regionGrid.GetValidRegionAt_NoRebuild(c)?.Room;
                if (room != null)
                    room.Notify_RoofChanged();
                //map.regionGrid.GetValidRegionAt_NoRebuild(c)?.District.Notify_RoofChanged(); This fixes the roofs notification instead of the 3 previous lines -Sernior
                if (__instance.drawerInt != null) __instance.drawerInt.SetDirty();

                map.mapDrawer.MapMeshDirty(c, MapMeshFlag.Roofs);
            }

            return false;
        }
    }
}