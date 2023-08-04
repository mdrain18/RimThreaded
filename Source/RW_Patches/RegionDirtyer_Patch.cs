using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class RegionDirtyer_Patch
    {
        [ThreadStatic] public static List<Region> regionsToDirty;

        public static Dictionary<RegionDirtyer, ConcurrentQueue<IntVec3>> dirtyCellsDict =
            new Dictionary<RegionDirtyer, ConcurrentQueue<IntVec3>>();

        public static object regionDirtyerLock = new object();

        internal static void InitializeThreadStatics()
        {
            regionsToDirty = new List<Region>();
        }

        public static void RunDestructivePatches()
        {
            var original = typeof(RegionDirtyer);
            var patched = typeof(RegionDirtyer_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(SetAllClean));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_WalkabilityChanged));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_ThingAffectingRegionsSpawned));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_ThingAffectingRegionsDespawned));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetAllDirty));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetRegionDirty));
        }

        public static bool SetAllClean(RegionDirtyer __instance)
        {
            lock (dirtyCellsDict)
            {
                var dirtyCells = get_DirtyCells(__instance);
                foreach (var dirtyCell in dirtyCells) __instance.map.temperatureCache.ResetCachedCellInfo(dirtyCell);
                dirtyCellsDict.SetOrAdd(__instance, new ConcurrentQueue<IntVec3>());
            }

            return false;
        }

        public static ConcurrentQueue<IntVec3> get_DirtyCells(RegionDirtyer __instance)
        {
            if (!dirtyCellsDict.TryGetValue(__instance, out var dirtyCells))
                lock (dirtyCellsDict)
                {
                    if (!dirtyCellsDict.TryGetValue(__instance, out var dirtyCells2))
                    {
                        dirtyCells2 = new ConcurrentQueue<IntVec3>();
                        dirtyCellsDict.SetOrAdd(__instance, dirtyCells2);
                    }

                    dirtyCells = dirtyCells2;
                }

            return dirtyCells;
        }

        public static bool Notify_WalkabilityChanged(RegionDirtyer __instance, IntVec3 c)
        {
            regionsToDirty.Clear();
            for (var i = 0; i < 9; i++)
            {
                var c2 = c + GenAdj.AdjacentCellsAndInside[i];
                if (c2.InBounds(__instance.map))
                {
                    var regionAt_NoRebuild_InvalidAllowed =
                        __instance.map.regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c2);
                    if (regionAt_NoRebuild_InvalidAllowed != null && regionAt_NoRebuild_InvalidAllowed.valid)
                    {
                        __instance.map.temperatureCache.TryCacheRegionTempInfo(c, regionAt_NoRebuild_InvalidAllowed);
                        regionsToDirty.Add(regionAt_NoRebuild_InvalidAllowed);
                    }
                }
            }

            for (var j = 0; j < regionsToDirty.Count; j++) SetRegionDirty(__instance, regionsToDirty[j]);

            //regionsToDirty.Clear();
            // why twice? (AdjacentCellsAndInside should include "c") 
            var dirtyCells = get_DirtyCells(__instance);
            if (c.Walkable(__instance.map))
                dirtyCells.Enqueue(c);
            /*
                lock (dirtyCells)
                {
                    if (!dirtyCells.Contains(c))
                    {
                        dirtyCells.Add(c);
                    }
                }
                */

            return false;
        }

        public static bool Notify_ThingAffectingRegionsSpawned(RegionDirtyer __instance, Thing b)
        {
            regionsToDirty.Clear();
            foreach (var item in b.OccupiedRect().ExpandedBy(1).ClipInsideMap(b.Map))
            {
                var validRegionAt_NoRebuild = b.Map.regionGrid.GetValidRegionAt_NoRebuild(item);
                if (validRegionAt_NoRebuild != null)
                {
                    b.Map.temperatureCache.TryCacheRegionTempInfo(item, validRegionAt_NoRebuild);
                    regionsToDirty.Add(validRegionAt_NoRebuild);
                }
            }

            for (var i = 0; i < regionsToDirty.Count; i++) SetRegionDirty(__instance, regionsToDirty[i]);
            return false;
        }


        public static bool Notify_ThingAffectingRegionsDespawned(RegionDirtyer __instance, Thing b)
        {
            regionsToDirty.Clear();
            var validRegionAt_NoRebuild = __instance.map.regionGrid.GetValidRegionAt_NoRebuild(b.Position);
            if (validRegionAt_NoRebuild != null)
            {
                __instance.map.temperatureCache.TryCacheRegionTempInfo(b.Position, validRegionAt_NoRebuild);
                regionsToDirty.Add(validRegionAt_NoRebuild);
            }

            foreach (var item2 in GenAdj.CellsAdjacent8Way(b))
                if (item2.InBounds(__instance.map))
                {
                    var validRegionAt_NoRebuild2 = __instance.map.regionGrid.GetValidRegionAt_NoRebuild(item2);
                    if (validRegionAt_NoRebuild2 != null)
                    {
                        __instance.map.temperatureCache.TryCacheRegionTempInfo(item2, validRegionAt_NoRebuild2);
                        regionsToDirty.Add(validRegionAt_NoRebuild2);
                    }
                }

            for (var i = 0; i < regionsToDirty.Count; i++) SetRegionDirty(__instance, regionsToDirty[i]);

            var dirtyCells = get_DirtyCells(__instance);
            if (b.def.size.x == 1 && b.def.size.z == 1)
            {
                dirtyCells.Enqueue(b.Position);
                return false;
            }

            var cellRect = b.OccupiedRect();
            for (var j = cellRect.minZ; j <= cellRect.maxZ; j++)
            for (var k = cellRect.minX; k <= cellRect.maxX; k++)
            {
                var item = new IntVec3(k, 0, j);
                dirtyCells.Enqueue(item);
            }

            return false;
        }

        public static bool SetAllDirty(RegionDirtyer __instance)
        {
            var dirtyCells = new ConcurrentQueue<IntVec3>();
            foreach (var item in __instance.map) dirtyCells.Enqueue(item);
            lock (dirtyCellsDict)
            {
                dirtyCellsDict.SetOrAdd(__instance, dirtyCells);
            }

            foreach (var item2 in __instance.map.regionGrid.AllRegions_NoRebuild_InvalidAllowed)
                SetRegionDirty(__instance, item2, false);
            return false;
        }

        public static bool SetRegionDirty(RegionDirtyer __instance, Region reg, bool addCellsToDirtyCells = true)
        {
            if (!reg.valid) return false;
            reg.valid = false;
            reg.District = null;
            var links = reg.links;
            for (var i = 0; i < links.Count; i++) links[i].Deregister(reg);

            reg.links = new List<RegionLink>();
            if (!addCellsToDirtyCells) return false;
            var dirtyCells = get_DirtyCells(__instance);
            foreach (var cell in reg.Cells)
            {
                //RegionAndRoomUpdater_Patch.cellsWithNewRegions.Remove(cell);
                dirtyCells.Enqueue(cell);
                if (DebugViewSettings.drawRegionDirties) __instance.map.debugDrawer.FlashCell(cell);
            }

            return false;
        }
    }
}