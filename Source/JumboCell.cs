using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static RimThreaded.RW_Patches.Area_Patch;

namespace RimThreaded
{
    internal static class JumboCell
    {
        private const float ZOOM_MULTIPLIER = 1.5f;
        [ThreadStatic] private static HashSet<IntVec3> retrunedThings;
        private static readonly IntVec3[] noOffset = {IntVec3.Zero};
        private static readonly List<int> zoomLevels = new List<int>();

        internal static void InitializeThreadStatics()
        {
            retrunedThings = new HashSet<IntVec3>();
        }

        public static void ReregisterObject(Map map, IntVec3 location, JumboCell_Cache jumboCell_Cache)
        {
            if (!location.IsValid)
                return;
            var positionsAwaitingAction = jumboCell_Cache.positionsAwaitingAction;
            // Try to remove an item that has a null map. Only way is to check all maps...
            if (map == null)
            {
                foreach (var kv in positionsAwaitingAction)
                {
                    var map2 = kv.Key;
                    if (map2 != null)
                        RemoveObjectFromAwaitingActionHashSets(map2, location, kv.Value);
                }

                return;
            }

            var awaitingActionZoomLevels =
                GetAwaitingActionsZoomLevels(jumboCell_Cache.positionsAwaitingAction, map, jumboCell_Cache);
            RemoveObjectFromAwaitingActionHashSets(map, location, awaitingActionZoomLevels);
            if (jumboCell_Cache.IsActionableObject(map, location))
                AddObjectToActionableObjects(map, location, awaitingActionZoomLevels);
        }

        public static int getJumboCellWidth(int zoomLevel)
        {
            if (zoomLevels.Count <= zoomLevel)
            {
                var lastZoomLevel = 1;
                for (var i = zoomLevels.Count; i <= zoomLevel; i++)
                {
                    if (i > 0)
                        lastZoomLevel = zoomLevels[i - 1];
                    zoomLevels.Add(Mathf.CeilToInt(lastZoomLevel * ZOOM_MULTIPLIER));
                }
            }

            return zoomLevels[zoomLevel];
        }

        public static int CellToIndexCustom(IntVec3 position, int mapSizeX, int jumboCellWidth)
        {
            var XposInJumboCell = position.x / jumboCellWidth;
            var ZposInJumboCell = position.z / jumboCellWidth;
            var jumboCellColumnsInMap = GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth);
            return CellXZToIndexCustom(XposInJumboCell, ZposInJumboCell, jumboCellColumnsInMap);
        }

        public static int NumGridCellsCustom(int mapSizeX, int mapSizeZ, int jumboCellWidth)
        {
            return GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth) *
                   Mathf.CeilToInt(mapSizeZ / (float) jumboCellWidth);
        }

        public static int GetJumboCellColumnsInMap(int mapSizeX, int jumboCellWidth)
        {
            return Mathf.CeilToInt(mapSizeX / (float) jumboCellWidth);
        }

        public static int CellXZToIndexCustom(int XposOfJumboCell, int ZposOfJumboCell, int jumboCellColumnsInMap)
        {
            return jumboCellColumnsInMap * ZposOfJumboCell + XposOfJumboCell;
        }

        public static IEnumerable<IntVec3> GetOptimalOffsetOrder(IntVec3 position, int zoomLevel, Range2D scannedRange,
            Range2D areaRange, int jumboCellWidth)
        {
            //optimization is a bit more costly for performance, but should help find "nearer" next jumbo cell to check
            var angle16 = GetAngle16(position, jumboCellWidth);
            foreach (var cardinalDirection in GetClosestDirections(angle16))
                switch (cardinalDirection)
                {
                    case 0:
                        if (scannedRange.maxZ < areaRange.maxZ)
                            yield return IntVec3.North;
                        break;

                    case 1:
                        if (scannedRange.maxZ < areaRange.maxZ && scannedRange.maxX < areaRange.maxX)
                            yield return IntVec3.NorthEast;
                        break;

                    case 2:
                        if (scannedRange.maxX < areaRange.maxX)
                            yield return IntVec3.East;
                        break;

                    case 3:
                        if (scannedRange.minZ > areaRange.minZ && scannedRange.maxX < areaRange.maxX)
                            yield return IntVec3.SouthEast;
                        break;

                    case 4:
                        if (scannedRange.minZ > areaRange.minZ)
                            yield return IntVec3.South;
                        break;

                    case 5:
                        if (scannedRange.minZ > areaRange.minZ && scannedRange.minX > areaRange.minX)
                            yield return IntVec3.SouthWest;
                        break;

                    case 6:
                        if (scannedRange.maxX < areaRange.maxX)
                            yield return IntVec3.West;
                        break;

                    case 7:
                        if (scannedRange.maxZ < areaRange.maxZ && scannedRange.minX > areaRange.minX)
                            yield return IntVec3.NorthWest;
                        break;
                }
        }

        public static int GetAngle16(IntVec3 position, int jumboCellWidth)
        {
            var relativeX = position.x % jumboCellWidth;
            var relativeZ = position.z % jumboCellWidth;
            var widthOffset = jumboCellWidth - 1;
            var cartesianX = relativeX * 2 - widthOffset;
            var cartesianZ = relativeZ * 2 - widthOffset;
            var slope2 = cartesianZ * 200 / (cartesianX * 100 + 1);
            if (cartesianX >= 0)
            {
                if (slope2 >= 0)
                {
                    if (slope2 >= 2)
                    {
                        if (slope2 >= 4)
                            return 0;
                        return 1;
                    }

                    if (slope2 >= 1)
                        return 2;
                    return 3;
                }

                if (slope2 <= -2)
                {
                    if (slope2 <= -4)
                        return 7;
                    return 6;
                }

                if (slope2 <= -1)
                    return 5;
                return 4;
            }

            if (slope2 >= 0)
            {
                if (slope2 >= 2)
                {
                    if (slope2 >= 4)
                        return 8;
                    return 9;
                }

                if (slope2 >= 1)
                    return 10;
                return 11;
            }

            if (slope2 <= -2)
            {
                if (slope2 <= -4)
                    return 15;
                return 14;
            }

            if (slope2 <= -1)
                return 13;
            return 12;
        }

        public static IEnumerable<int> GetClosestDirections(int startingPosition)
        {
            var starting8 = (startingPosition + 1) / 2 % 8;
            yield return starting8;
            var startingDirection = startingPosition % 2;
            switch (startingDirection)
            {
                case 0:
                    yield return (starting8 + 1) % 8;
                    yield return (starting8 + 7) % 8;
                    yield return (starting8 + 2) % 8;
                    yield return (starting8 + 6) % 8;
                    yield return (starting8 + 3) % 8;
                    yield return (starting8 + 5) % 8;
                    yield return (starting8 + 4) % 8;
                    break;
                case 1:
                    yield return (starting8 + 7) % 8;
                    yield return (starting8 + 1) % 8;
                    yield return (starting8 + 6) % 8;
                    yield return (starting8 + 2) % 8;
                    yield return (starting8 + 3) % 8;
                    yield return (starting8 + 4) % 8;
                    yield return (starting8 + 5) % 8;
                    break;
            }
        }

        public static IEnumerable<IntVec3> GetClosestActionableLocations(Pawn pawn, Map map,
            JumboCell_Cache jumboCell_Cache)
        {
            int jumboCellWidth;
            int XposOfJumboCell;
            int ZposOfJumboCell;
            var mapSizeX = map.Size.x;
            retrunedThings.Clear();
            IntVec3[] objectsAtCellCopy;
            var awaitingActionZoomLevels =
                GetAwaitingActionsZoomLevels(jumboCell_Cache.positionsAwaitingAction, map, jumboCell_Cache);
            var position = pawn.Position;
            var effectiveAreaRestrictionInPawnCurrentMap = pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
            var areaRange = GetCorners(effectiveAreaRestrictionInPawnCurrentMap);
            var scannedRange = new Range2D(position.x, position.z, position.x, position.z);
            for (var zoomLevel = 0; zoomLevel < awaitingActionZoomLevels.Count; zoomLevel++)
            {
                var objectsGrid = awaitingActionZoomLevels[zoomLevel];
                jumboCellWidth = getJumboCellWidth(zoomLevel);
                var jumboCellColumnsInMap = GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth);
                XposOfJumboCell = position.x / jumboCellWidth;
                ZposOfJumboCell = position.z / jumboCellWidth; //assuming square map
                IEnumerable<IntVec3> offsetOrder;
                if (zoomLevel == 0)
                    offsetOrder = noOffset;
                else
                    offsetOrder = GetOptimalOffsetOrder(position, zoomLevel, scannedRange, areaRange, jumboCellWidth);
                foreach (var offset in offsetOrder)
                {
                    var newXposOfJumboCell = XposOfJumboCell + offset.x;
                    var newZposOfJumboCell = ZposOfJumboCell + offset.z;
                    if (newXposOfJumboCell >= 0 && newXposOfJumboCell < jumboCellColumnsInMap &&
                        newZposOfJumboCell >= 0 && newZposOfJumboCell < jumboCellColumnsInMap)
                    {
                        var jumboCellIndex = CellXZToIndexCustom(newXposOfJumboCell, newZposOfJumboCell,
                            jumboCellColumnsInMap);
                        var thingsAtCell = objectsGrid[jumboCellIndex];
                        if (thingsAtCell != null && thingsAtCell.Count > 0)
                        {
                            objectsAtCellCopy = thingsAtCell.ToArray();
                            foreach (var actionableObject in objectsAtCellCopy)
                                if (!retrunedThings.Contains(actionableObject))
                                {
                                    yield return actionableObject;
                                    retrunedThings.Add(actionableObject);
                                }
                        }
                    }
                }

                scannedRange.minX = Math.Min(scannedRange.minX, (XposOfJumboCell - 1) * jumboCellWidth);
                scannedRange.minZ = Math.Min(scannedRange.minZ, (ZposOfJumboCell - 1) * jumboCellWidth);
                scannedRange.maxX = Math.Max(scannedRange.maxX, (XposOfJumboCell + 2) * jumboCellWidth - 1);
                scannedRange.maxZ = Math.Max(scannedRange.maxZ, (ZposOfJumboCell + 2) * jumboCellWidth - 1);
            }
        }

        public static void RemoveObjectFromAwaitingActionHashSets(Map map, IntVec3 location,
            List<HashSet<IntVec3>[]> awaitingActionZoomLevels)
        {
            if (map == null || map.info == null)
                return;
            var size = map.Size;
            if (size == null)
                return;
            int jumboCellWidth;
            var mapSizeX = size.x;
            var mapSizeZ = size.z;
            int zoomLevel;
            zoomLevel = 0;
            do
            {
                jumboCellWidth = getJumboCellWidth(zoomLevel);
                var cellIndex = CellToIndexCustom(location, mapSizeX, jumboCellWidth);
                var hashset = awaitingActionZoomLevels[zoomLevel][cellIndex];
                if (hashset != null)
                    lock (hashset)
                    {
                        hashset.Remove(location);
                    }

                zoomLevel++;
            } while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
        }

        public static List<HashSet<IntVec3>[]> GetAwaitingActionsZoomLevels(
            Dictionary<Map, List<HashSet<IntVec3>[]>> awaitingActionMapDict, Map map, JumboCell_Cache jumboCell_Cache)
        {
            if (!awaitingActionMapDict.TryGetValue(map, out var awaitingActionsZoomLevels))
                lock (awaitingActionMapDict)
                {
                    if (!awaitingActionMapDict.TryGetValue(map, out var awaitingActionsZoomLevels2))
                    {
                        Log.Message("RimThreaded is caching Cells...");
                        awaitingActionsZoomLevels2 = new List<HashSet<IntVec3>[]>();
                        var mapSizeX = map.Size.x;
                        var mapSizeZ = map.Size.z;
                        int jumboCellWidth;
                        var zoomLevel = 0;
                        do
                        {
                            jumboCellWidth = getJumboCellWidth(zoomLevel);
                            var numGridCells = NumGridCellsCustom(mapSizeX, mapSizeZ, jumboCellWidth);
                            awaitingActionsZoomLevels2.Add(new HashSet<IntVec3>[numGridCells]);
                            zoomLevel++;
                        } while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);

                        var zones = map.zoneManager.AllZones;
                        foreach (var zone in zones)
                            if (zone is Zone_Growing)
                                foreach (var c in zone.Cells)
                                    //AddObjectToActionableObjects(zone.Map, c, awaitingActionsZoomLevels2);
                                    if (jumboCell_Cache.IsActionableObject(zone.Map, c))
                                        AddObjectToActionableObjects(zone.Map, c, awaitingActionsZoomLevels2);
                        awaitingActionMapDict[map] = awaitingActionsZoomLevels2;
                    }

                    awaitingActionsZoomLevels = awaitingActionsZoomLevels2;
                }

            return awaitingActionsZoomLevels;
        }

        public static void AddObjectToActionableObjects(Map map, IntVec3 location,
            List<HashSet<IntVec3>[]> awaitingActionZoomLevels)
        {
            int jumboCellWidth;
            var mapSizeX = map.Size.x;
            var mapSizeZ = map.Size.z;
            int zoomLevel;
            zoomLevel = 0;
            do
            {
                jumboCellWidth = getJumboCellWidth(zoomLevel);
                var awaitingActionGrid = awaitingActionZoomLevels[zoomLevel];
                var jumboCellIndex = CellToIndexCustom(location, mapSizeX, jumboCellWidth);
                var hashset = awaitingActionGrid[jumboCellIndex];
                if (hashset == null)
                {
                    hashset = new HashSet<IntVec3>();
                    lock (awaitingActionGrid)
                    {
                        awaitingActionGrid[jumboCellIndex] = hashset;
                    }
                }

                lock (hashset)
                {
                    hashset.Add(location);
                }

                zoomLevel++;
            } while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
        }
    }
}