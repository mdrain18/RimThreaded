using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class RegionAndRoomUpdater_Patch
    {
        [ThreadStatic] public static bool working;
        [ThreadStatic] public static HashSet<Room> tmpVisitedRooms;
        [ThreadStatic] public static ConcurrentQueue<IntVec3> cellsToReset;

        private static readonly Type original = typeof(RegionAndRoomUpdater);
        private static readonly Type patched = typeof(RegionAndRoomUpdater_Patch);

        public static HashSet<IntVec3> cellsWithNewRegions = new HashSet<IntVec3>();
        public static List<Region> regionsToReDirty = new List<Region>();

        internal static void InitializeThreadStatics()
        {
            cellsToReset = new ConcurrentQueue<IntVec3>();
        }

        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, nameof(TryRebuildDirtyRegionsAndRooms));
        }

        public static bool TryRebuildDirtyRegionsAndRooms(RegionAndRoomUpdater __instance)
        {
            if (!__instance.Enabled || working) return false;
            working = true;
            if (!__instance.initialized)
                lock (__instance)
                {
                    __instance.RebuildAllRegionsAndRooms();
                }

            var dirtyCells = RegionDirtyer_Patch.get_DirtyCells(__instance.map.regionDirtyer);
            if (dirtyCells.Count == 0)
            {
                working = false;
                return false;
            }

            lock (__instance)
            {
                try
                {
                    RegenerateNewRegionsFromDirtyCells2(__instance, dirtyCells);

                    __instance.CreateOrUpdateRooms();
                    //1. FloodAndSetNewRegionIndex (newRegions)
                    //2. CreateOrAttachToExistingDistricts
                    //   add each (newRegion) to (currentRegionGroup)
                    //   add each (currentRegionGroup) to (newDistricts)/(reusedOldDistricts)
                    //3. CombineNewAndReusedDistrictsIntoContiguousRooms
                    //   foreach each (newDistricts)/(reusedOldDistricts).newOrReusedRoomIndex, update(district.Neighbors.newOrReusedRoomIndex)
                    //4. CreateOrAttachToExistingRooms
                    //   add each (reusedOldDistrict) to (currentDistrictGroup)
                    //   add each (newDistrict) to (currentDistrictGroup)
                    //   add each (currentDistrictGroup.RoomNeighbor) to (newRooms)/(reusedOldRooms)
                    //5. NotifyAffectedDistrictsAndRoomsAndUpdateTemperature
                }
                catch (Exception arg)
                {
                    Log.Error("Exception while rebuilding dirty regions: " + arg);
                }

                while (cellsToReset.TryDequeue(out var dirtyCell))
                    __instance.map.temperatureCache.ResetCachedCellInfo(dirtyCell);
                __instance.initialized = true;
                working = false;
            }
            //regionCleaning.Set();

            if (DebugSettings.detectRegionListersBugs) Autotests_RegionListers.CheckBugs(__instance.map);
            return false;
        }

        private static void RegenerateNewRegionsFromDirtyCells2(RegionAndRoomUpdater __instance,
            ConcurrentQueue<IntVec3> dirtyCells)
        {
            cellsWithNewRegions.Clear();
            var newRegions = __instance.newRegions;
            newRegions.Clear();
            var localMap = __instance.map;
            while (dirtyCells.TryDequeue(out var dirtyCell)) cellsToReset.Enqueue(dirtyCell);
            foreach (var dirtyCell in cellsToReset)
            {
                var oldRegion = dirtyCell.GetRegion(localMap, RegionType.Set_All);
                if (oldRegion == null)
                {
                    var region = localMap.regionMaker.TryGenerateRegionFrom(dirtyCell);
                    if (region != null) newRegions.Add(region);
                }
            }
        }
    }
}