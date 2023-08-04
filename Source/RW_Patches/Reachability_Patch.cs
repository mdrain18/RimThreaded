using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    public class Reachability_Patch
    {
        [ThreadStatic] public static Queue<Region> openQueue;
        [ThreadStatic] public static List<Region> destRegions;
        [ThreadStatic] public static List<Region> startingRegions;
        [ThreadStatic] public static HashSet<Region> regionsReached; //newly defined

        public static void InitializeThreadStatics()
        {
            regionsReached = new HashSet<Region>();
            openQueue = new Queue<Region>();
            destRegions = new List<Region>();
            startingRegions = new List<Region>();
        }

        public static void RunDestructivePatches()
        {
            var original = typeof(Reachability);
            var patched = typeof(Reachability_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(CanReach),
                new[] {typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms)});
        }

        private static void QueueNewOpenRegion(Region region, Queue<Region> openQueueParam,
            HashSet<Region> regionsReached)
        {
            if (region == null)
            {
                //Log.ErrorOnce("Tried to queue null region.", 881121);
                Log.Error("Tried to queue null region.");
                return;
            }

            if (regionsReached.Contains(region))
            {
                Log.ErrorOnce("Region is already reached; you can't open it. Region: " + region, 719991);
                return;
            }

            openQueueParam.Enqueue(region);
            regionsReached.Add(region);
        }

        public static bool CanReach(Reachability __instance, ref bool __result, IntVec3 start, LocalTargetInfo dest,
            PathEndMode peMode, TraverseParms traverseParams)
        {
            var map = __instance.map;
            //if (working)
            //{
            //Log.ErrorOnce("Called CanReach() while working. This should never happen. Suppressing further errors.", 7312233);
            //return false;
            //}

            if (traverseParams.pawn != null)
            {
                if (!traverseParams.pawn.Spawned)
                {
                    __result = false;
                    return false;
                }

                if (traverseParams.pawn.Map != map)
                {
                    Log.Error(string.Concat(
                        "Called CanReach() with a pawn spawned not on this map. This means that we can't check his reachability here. Pawn's current map should have been used instead of this one. pawn=",
                        traverseParams.pawn, " pawn.Map=", traverseParams.pawn.Map, " map=", map));
                    __result = false;
                    return false;
                }
            }

            if (ReachabilityImmediate.CanReachImmediate(start, dest, map, peMode, traverseParams.pawn))
            {
                __result = true;
                return false;
            }

            if (!dest.IsValid)
            {
                __result = false;
                return false;
            }

            if (dest.HasThing && dest.Thing.Map != map)
            {
                __result = false;
                return false;
            }

            if (!start.InBounds(map) || !dest.Cell.InBounds(map))
            {
                __result = false;
                return false;
            }

            if ((peMode == PathEndMode.OnCell || peMode == PathEndMode.Touch || peMode == PathEndMode.ClosestTouch) &&
                traverseParams.mode != TraverseMode.NoPassClosedDoorsOrWater &&
                traverseParams.mode != TraverseMode.PassAllDestroyableThingsNotWater)
            {
                var district = RegionAndRoomQuery.DistirctAtFast(start, map);
                if (district != null && district == RegionAndRoomQuery.DistirctAtFast(dest.Cell, map))
                {
                    __result = true;
                    return false;
                }
            }

            if (traverseParams.mode == TraverseMode.PassAllDestroyableThings)
            {
                var traverseParams2 = traverseParams;
                traverseParams2.mode = TraverseMode.PassDoors;
                var canReachResult = false;
                CanReach(__instance, ref canReachResult, start, dest, peMode, traverseParams2);
                if (canReachResult)
                {
                    __result = true;
                    return false;
                }
            }

            dest = (LocalTargetInfo) GenPath.ResolvePathMode(traverseParams.pawn, dest.ToTargetInfo(map), ref peMode);
            //working = true;
            __instance.pathGrid = map.pathing.For(traverseParams).pathGrid;
            var pathGrid = __instance.pathGrid;
            __instance.regionGrid = map.regionGrid;
            var regionGrid = map.regionGrid;

            destRegions.Clear();

            switch (peMode)
            {
                case PathEndMode.OnCell:
                {
                    var region = dest.Cell.GetRegion(map);
                    if (region != null && region.Allows(traverseParams, true)) destRegions.Add(region);
                    break;
                }
                case PathEndMode.Touch:
                    TouchPathEndModeUtility.AddAllowedAdjacentRegions(dest, traverseParams, map, destRegions);
                    break;
            }

            if (destRegions.Count == 0 && traverseParams.mode != TraverseMode.PassAllDestroyableThings &&
                traverseParams.mode != TraverseMode.PassAllDestroyableThingsNotWater)
            {
                __result = false;
                return false;
            }

            destRegions.RemoveDuplicates();
            regionsReached.Clear();
            openQueue.Clear();
            startingRegions.Clear();

            DetermineStartRegions(map, start, startingRegions, pathGrid, regionGrid, openQueue, regionsReached);
            if (openQueue.Count == 0 && traverseParams.mode != TraverseMode.PassAllDestroyableThings &&
                traverseParams.mode != TraverseMode.PassAllDestroyableThingsNotWater)
            {
                __result = false;
                return false;
            }

            if (startingRegions.Any() && destRegions.Any() && __instance.CanUseCache(traverseParams.mode))
                switch (GetCachedResult(__instance, traverseParams, startingRegions, destRegions))
                {
                    case BoolUnknown.True:
                        __result = true;
                        return false;
                    case BoolUnknown.False:
                        __result = false;
                        return false;
                }

            if (traverseParams.mode == TraverseMode.PassAllDestroyableThings ||
                traverseParams.mode == TraverseMode.PassAllDestroyableThingsNotWater ||
                traverseParams.mode == TraverseMode.NoPassClosedDoorsOrWater)
            {
                var result = __instance.CheckCellBasedReachability(start, dest, peMode, traverseParams);
                __result = result;
                return false;
            }

            var result2 = CheckRegionBasedReachability(__instance, traverseParams, openQueue, regionsReached,
                startingRegions, destRegions);
            __result = result2;
            return false;
        }

        private static BoolUnknown GetCachedResult(Reachability __instance, TraverseParms traverseParams,
            List<Region> startingRegionsParams, List<Region> destRegionsParams)
        {
            var flag = false;
            var cache = __instance.cache;
            for (var i = 0; i < startingRegionsParams.Count; i++)
            for (var j = 0; j < destRegionsParams.Count; j++)
            {
                if (destRegionsParams[j] == startingRegionsParams[i]) return BoolUnknown.True;
                switch (cache.CachedResultFor(startingRegionsParams[i].District, destRegionsParams[j].District,
                            traverseParams))
                {
                    case BoolUnknown.True:
                        return BoolUnknown.True;
                    case BoolUnknown.Unknown:
                        flag = true;
                        break;
                }
            }

            if (!flag) return BoolUnknown.False;

            return BoolUnknown.Unknown;
        }

        private static bool CheckRegionBasedReachability(Reachability __instance, TraverseParms traverseParams,
            Queue<Region> openQueueParam,
            HashSet<Region> regionsReached, List<Region> startingRegionsParam, List<Region> destRegionsParam)
        {
            var cache = __instance.cache;
            while (openQueueParam.Count > 0)
            {
                var region = openQueueParam.Dequeue();
                for (var i = 0; i < region.links.Count; i++)
                {
                    var regionLink = region.links[i];
                    for (var j = 0; j < 2; j++)
                    {
                        var region2 = regionLink.regions[j];
                        if (region2 == null || regionsReached.Contains(region2) || !region2.type.Passable() ||
                            !region2.Allows(traverseParams, false)) continue;

                        if (destRegionsParam.Contains(region2))
                        {
                            for (var k = 0; k < startingRegionsParam.Count; k++)
                                cache.AddCachedResult(startingRegionsParam[k].District, region2.District,
                                    traverseParams, true);

                            return true;
                        }

                        QueueNewOpenRegion(region2, openQueueParam, regionsReached);
                    }
                }
            }

            for (var l = 0; l < startingRegionsParam.Count; l++)
            for (var m = 0; m < destRegionsParam.Count; m++)
                cache.AddCachedResult(startingRegionsParam[l].District, destRegionsParam[m].District, traverseParams,
                    false);
            return false;
        }

        private static void DetermineStartRegions(Map map, IntVec3 start, List<Region> startingRegionsParam,
            PathGrid pathGrid,
            RegionGrid regionGrid, Queue<Region> openQueueParam, HashSet<Region> regionsReached)
        {
            startingRegionsParam.Clear();
            if (pathGrid.WalkableFast(start))
            {
                var validRegionAt = regionGrid.GetValidRegionAt(start);
                if (validRegionAt != null && !regionsReached.Contains(validRegionAt))
                {
                    QueueNewOpenRegion(validRegionAt, openQueueParam, regionsReached);
                    startingRegionsParam.Add(validRegionAt);
                }

                return;
            }

            for (var index = 0; index < 8; ++index)
            {
                var intVec = start + GenAdj.AdjacentCells[index];
                if (intVec.InBounds(map) && pathGrid.WalkableFast(intVec))
                {
                    var validRegionAt2 = regionGrid.GetValidRegionAt(intVec);
                    if (validRegionAt2 != null && !regionsReached.Contains(validRegionAt2))
                    {
                        QueueNewOpenRegion(validRegionAt2, openQueueParam, regionsReached);
                        startingRegionsParam.Add(validRegionAt2);
                    }
                }
            }
        }
    }
}