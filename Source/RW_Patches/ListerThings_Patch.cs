using System;
using System.Collections.Generic;
using Verse;
using static RimThreaded.RW_Patches.Area_Patch;
using static RimThreaded.JumboCell;

namespace RimThreaded.RW_Patches
{
    public class ListerThings_Patch
    {
        public const float
            ZOOM_MULTIPLIER =
                1.5f; //must be greater than 1. lower numbers will make searches slower, but ensure pawns find the closer things first.

        public static Dictionary<ThingRequestGroup, HashSet<Thing>> thingsRegistered =
            new Dictionary<ThingRequestGroup, HashSet<Thing>>();

        public static Dictionary<ThingDef, List<ThingRequestGroup>> thingDefThingGroups =
            new Dictionary<ThingDef, List<ThingRequestGroup>>();

        public static List<ThingRequestGroup> workGroups = new List<ThingRequestGroup>
        {
            ThingRequestGroup.Seed,
            ThingRequestGroup.Blueprint,
            ThingRequestGroup.Refuelable,
            ThingRequestGroup.Transporter,
            ThingRequestGroup.BuildingFrame,
            ThingRequestGroup.PotentialBillGiver,
            ThingRequestGroup.Filth
            //ThingRequestGroup.BuildingArtificial
        };

        public static List<int> zoomLevels = new List<int>();

        public static Dictionary<Map, Dictionary<ThingRequestGroup, List<HashSet<Thing>[]>>>
            mapToGroupToZoomsToGridToThings =
                new Dictionary<Map, Dictionary<ThingRequestGroup, List<HashSet<Thing>[]>>>();

        // Map, (jumbo cell zoom level, #0 item=zoom 2x2, #1 item=4x4), jumbo cell index converted from x,z coord, HashSet<Thing>
        [ThreadStatic] private static HashSet<Thing> returnedThings;

        public static void RunDestructivePatches()
        {
            var original = typeof(ListerThings);
            var patched = typeof(ListerThings_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(Remove));
            RimThreadedHarmony.Prefix(original, patched, nameof(Add));
            //RimThreadedHarmony.Postfix(original, patched, nameof(ThingsMatching));
            //RimThreadedHarmony.Postfix(original, patched, nameof(get_AllThings));
        }

        public static void get_AllThings(ListerThings __instance, ref List<Thing> __result)
        {
            if (__result != null)
                lock (__instance)
                {
                    var tmp = __result;
                    __result = new List<Thing>(tmp);
                    //__result. AddRange(tmp);
                    //__result = new List<Thing>(__result);
                }
        }

        public static void
            ThingsMatching(ListerThings __instance, ref List<Thing> __result,
                ThingRequest req) //this has to give a snapshot not just a reference -Sernior
        {
            if (__result != null)
                lock (__instance)
                {
                    var tmp = __result;
                    __result = new List<Thing>(tmp);
                    //__result.Clear();
                    //__result.AddRange(tmp);
                    //__result = new List<Thing>(__result);
                }
        }


        public static bool Add(ListerThings __instance, Thing t)
        {
            var thingDef = t.def;
            if (!ListerThings.EverListable(thingDef, __instance.use)) return false;

            lock (__instance)
            {
                if (!__instance.listsByDef.TryGetValue(thingDef, out var value))
                {
                    value = new List<Thing>();
                    __instance.listsByDef.Add(t.def, value);
                }

                value.Add(t);
            }

            var allGroups = ThingListGroupHelper.AllGroups;
            foreach (var thingRequestGroup in allGroups)
                if ((__instance.use != ListerThingsUse.Region || thingRequestGroup.StoreInRegion()) &&
                    thingRequestGroup.Includes(thingDef))
                    lock (__instance)
                    {
                        var list = __instance.listsByGroup[(uint) thingRequestGroup];
                        if (list == null)
                        {
                            list = new List<Thing>();
                            __instance.listsByGroup[(uint) thingRequestGroup] = list;
                            __instance.stateHashByGroup[(uint) thingRequestGroup] = 0;
                        }

                        list.Add(t);
                        __instance.stateHashByGroup[(uint) thingRequestGroup]++;
                    }

            return false;
        }

        public static void RegisterListerThing(Thing t)
        {
            var thingDef = t.def;
            var thingRequestGroups = GetThingRequestGroup(thingDef);
            foreach (var thingRequestGroup in thingRequestGroups)
                /*
                    lock(thingsRegistered)
                    {
                        if(!thingsRegistered.TryGetValue(thingRequestGroup, out HashSet<Thing> hashset))
                        {
                            hashset = new HashSet<Thing>();
                            thingsRegistered[thingRequestGroup] = hashset;
                        }
                        hashset.Add(t);
                    }
                    */
                AddThingToHashSets(t, thingRequestGroup);
            /*
            if (ThingRequestGroup.Bed.Includes(thingDef))
            {
                Building_Bed bed = t as Building_Bed;
                if(bed.OwnersForReading.Count == 0)
                    AddThingToHashSets(t, ThingRequestGroup.Bed);
            }
            */
        }

        public static bool Remove(ListerThings __instance, Thing t)
        {
            var thingDef = t.def;
            if (!ListerThings.EverListable(thingDef, __instance.use)) return false;
            lock (__instance)
            {
                var newListsByDef = new List<Thing>(__instance.listsByDef[thingDef]);
                newListsByDef.Remove(t);
                __instance.listsByDef[thingDef] = newListsByDef;
            }

            var allGroups = ThingListGroupHelper.AllGroups;
            for (var i = 0; i < allGroups.Length; i++)
            {
                var thingRequestGroup = allGroups[i];
                if ((__instance.use != ListerThingsUse.Region || thingRequestGroup.StoreInRegion()) &&
                    thingRequestGroup.Includes(thingDef))
                    lock (__instance)
                    {
                        var newListsByGroup = new List<Thing>(__instance.listsByGroup[i]);
                        newListsByGroup.Remove(t);
                        __instance.listsByGroup[i] = newListsByGroup;
                        __instance.stateHashByGroup[(uint) thingRequestGroup]++;
                    }
            }

            return false;
        }

        public static void DeregisterListerThing(Thing t)
        {
            var thingDef = t.def;
            var thingRequestGroups = GetThingRequestGroup(thingDef);
            foreach (var thingRequestGroup in thingRequestGroups) RemoveThingFromHashSets(t, thingRequestGroup);
        }

        private static IEnumerable<ThingRequestGroup> GetThingRequestGroup(ThingDef thingDef)
        {
            foreach (var thingRequestGroup in workGroups)
                if (thingRequestGroup.Includes(thingDef))
                    yield return thingRequestGroup;
            /*
            //TODO create cache using thingDefThingGroups?
            if (!thingDefThingGroups.TryGetValue(thingDef, out List<ThingRequestGroup> groups))
            {
                lock (thingDefThingGroups)
                {
                    if (!thingDefThingGroups.TryGetValue(thingDef, out List<ThingRequestGroup> groups2))
                    {
                        groups2 = new List<ThingRequestGroup>();
                        foreach (ThingRequestGroup thingRequestGroup in ThingListGroupHelper.AllGroups)
                        {
                            if (thingRequestGroup.Includes(thingDef))
                            {
                                groups2.Add(thingRequestGroup);
                            }
                        }
                        thingDefThingGroups[thingDef] = groups2;
                    }
                    groups = groups2;
                }
            }
            return groups;
            */
        }

        internal static void InitializeThreadStatics()
        {
            returnedThings = new HashSet<Thing>();
        }

        private static List<HashSet<Thing>[]> GetThingsZoomLevels(Map map, ThingRequest thingReq)
        {
            if (thingReq.singleDef == null) return GetZoomsFromGroup(map, thingReq.@group);
            return null;
        }

        private static List<HashSet<Thing>[]> GetZoomsFromGroup(Map map, ThingRequestGroup group)
        {
            List<HashSet<Thing>[]> zoomsOfGridOfThingsets = null;
            if (!mapToGroupToZoomsToGridToThings.TryGetValue(map, out var groupToZoomsToGridToThings))
                lock (mapToGroupToZoomsToGridToThings)
                {
                    if (!mapToGroupToZoomsToGridToThings.TryGetValue(map, out var groupToZoomsToGridToThings2))
                    {
                        groupToZoomsToGridToThings2 =
                            new Dictionary<ThingRequestGroup, List<HashSet<Thing>[]>>();
                        mapToGroupToZoomsToGridToThings[map] = groupToZoomsToGridToThings2;
                    }

                    groupToZoomsToGridToThings = groupToZoomsToGridToThings2;
                }

            zoomsOfGridOfThingsets =
                GetZoomsOfGridOfThingsetsFromGroup(groupToZoomsToGridToThings, group, map.Size.x, map.Size.z);
            return zoomsOfGridOfThingsets;
        }

        private static List<HashSet<Thing>[]> GetZoomsOfGridOfThingsets(
            Dictionary<ThingRequestGroup, List<HashSet<Thing>[]>> groupToZoomsToGridToThings,
            ThingRequestGroup group)
        {
            if (!groupToZoomsToGridToThings.TryGetValue(group, out var zoomsOfGridOfThings))
                lock (groupToZoomsToGridToThings)
                {
                    if (!groupToZoomsToGridToThings.TryGetValue(@group, out var thingsZoomLevels2))
                    {
                        thingsZoomLevels2 = new List<HashSet<Thing>[]>();
                        groupToZoomsToGridToThings[@group] = thingsZoomLevels2;
                    }

                    zoomsOfGridOfThings = thingsZoomLevels2;
                }

            return zoomsOfGridOfThings;
        }

        private static List<HashSet<Thing>[]> GetZoomsOfGridOfThingsetsFromGroup(
            Dictionary<ThingRequestGroup, List<HashSet<Thing>[]>> groupToZoomsToGridToThings,
            ThingRequestGroup group, int mapSizeX, int mapSizeZ)
        {
            var zoomsOfGridOfThingsets = GetZoomsOfGridOfThingsets(groupToZoomsToGridToThings, group);
            int jumboCellWidth;
            var zoomLevel = 0;
            do
            {
                jumboCellWidth = getJumboCellWidth(zoomLevel);
                var numGridCells = NumGridCellsCustom(mapSizeX, mapSizeZ, jumboCellWidth);
                zoomsOfGridOfThingsets.Add(new HashSet<Thing>[numGridCells]);
                zoomLevel++;
            } while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);

            return zoomsOfGridOfThingsets;
        }

        private static int CellToIndexCustom1(IntVec3 position, int mapSizeX, int jumboCellWidth)
        {
            var XposInJumboCell = position.x / jumboCellWidth;
            var ZposInJumboCell = position.z / jumboCellWidth;
            var jumboCellColumnsInMap = GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth);
            return CellToIndexCustom3(XposInJumboCell, ZposInJumboCell, jumboCellColumnsInMap);
        }

        internal static IEnumerable<Thing> GetClosestThingRequestGroupPosition(IntVec3 root, Map map,
            ThingRequestGroup bed)
        {
            var mapSizeX = map.Size.x;
            returnedThings.Clear(); //hashset used to ensure same item is not retured twice

            var zoomLevelsOfThings = GetZoomsFromGroup(map, bed);
            var position = root;
            Area effectiveAreaRestrictionInPawnCurrentMap = null;
            var areaRange = GetCorners(effectiveAreaRestrictionInPawnCurrentMap);
            var scannedRange = new Range2D(position.x, position.z, position.x, position.z);
            for (var zoomLevel = 0; zoomLevel < zoomLevelsOfThings.Count; zoomLevel++)
            {
                var thingsGrid = zoomLevelsOfThings[zoomLevel];
                var jumboCellWidth = getJumboCellWidth(zoomLevel);
                var jumboCellColumnsInMap = GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth);
                var XposOfJumboCell = position.x / jumboCellWidth;
                var ZposOfJumboCell = position.z / jumboCellWidth; //assuming square map
                if (zoomLevel ==
                    0) //this is needed to grab the center (9th square) otherwise, only the outside 8 edges/corners are normally needed
                    foreach (var haulableThing in GetThingsAtCellCopy(XposOfJumboCell, ZposOfJumboCell,
                                 jumboCellColumnsInMap, thingsGrid))
                        yield return haulableThing;
                var offsetOrder = GetOptimalOffsetOrder(position, zoomLevel, scannedRange, areaRange,
                    jumboCellWidth);
                foreach (var offset in offsetOrder)
                {
                    var newXposOfJumboCell = XposOfJumboCell + offset.x;
                    var newZposOfJumboCell = ZposOfJumboCell + offset.z;
                    if (newXposOfJumboCell >= 0 && newXposOfJumboCell < jumboCellColumnsInMap &&
                        newZposOfJumboCell >= 0 && newZposOfJumboCell < jumboCellColumnsInMap)
                        foreach (var haulableThing in GetThingsAtCellCopy(XposOfJumboCell, ZposOfJumboCell,
                                     jumboCellColumnsInMap, thingsGrid))
                            yield return haulableThing;
                }

                scannedRange.minX = Math.Min(scannedRange.minX, (XposOfJumboCell - 1) * jumboCellWidth);
                scannedRange.minZ = Math.Min(scannedRange.minZ, (ZposOfJumboCell - 1) * jumboCellWidth);
                scannedRange.maxX = Math.Max(scannedRange.maxX, (XposOfJumboCell + 2) * jumboCellWidth - 1);
                scannedRange.maxZ = Math.Max(scannedRange.maxZ, (ZposOfJumboCell + 2) * jumboCellWidth - 1);
            }
        }

        private static int CellToIndexCustom3(int XposOfJumboCell, int ZposOfJumboCell,
            int jumboCellColumnsInMap)
        {
            return jumboCellColumnsInMap * ZposOfJumboCell + XposOfJumboCell;
        }

        public static IEnumerable<Thing> GetClosestThingRequestGroup(Pawn pawn, Map map, ThingRequest thingReq)
        {
            var mapSizeX = map.Size.x;
            returnedThings.Clear(); //hashset used to ensure same item is not retured twice

            var zoomLevelsOfThings = GetThingsZoomLevels(map, thingReq);
            var position = pawn.Position;
            var effectiveAreaRestrictionInPawnCurrentMap =
                pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
            var areaRange = GetCorners(effectiveAreaRestrictionInPawnCurrentMap);
            var scannedRange = new Range2D(position.x, position.z, position.x, position.z);
            for (var zoomLevel = 0; zoomLevel < zoomLevelsOfThings.Count; zoomLevel++)
            {
                var thingsGrid = zoomLevelsOfThings[zoomLevel];
                var jumboCellWidth = getJumboCellWidth(zoomLevel);
                var jumboCellColumnsInMap = GetJumboCellColumnsInMap(mapSizeX, jumboCellWidth);
                var XposOfJumboCell = position.x / jumboCellWidth;
                var ZposOfJumboCell = position.z / jumboCellWidth; //assuming square map
                if (zoomLevel ==
                    0) //this is needed to grab the center (9th square) otherwise, only the outside 8 edges/corners are normally needed
                    foreach (var haulableThing in GetThingsAtCellCopy(XposOfJumboCell, ZposOfJumboCell,
                                 jumboCellColumnsInMap, thingsGrid))
                        yield return haulableThing;
                var offsetOrder = GetOptimalOffsetOrder(position, zoomLevel, scannedRange, areaRange,
                    jumboCellWidth);
                foreach (var offset in offsetOrder)
                {
                    var newXposOfJumboCell = XposOfJumboCell + offset.x;
                    var newZposOfJumboCell = ZposOfJumboCell + offset.z;
                    if (newXposOfJumboCell >= 0 && newXposOfJumboCell < jumboCellColumnsInMap &&
                        newZposOfJumboCell >= 0 && newZposOfJumboCell < jumboCellColumnsInMap)
                        foreach (var haulableThing in GetThingsAtCellCopy(XposOfJumboCell, ZposOfJumboCell,
                                     jumboCellColumnsInMap, thingsGrid))
                            yield return haulableThing;
                }

                scannedRange.minX = Math.Min(scannedRange.minX, (XposOfJumboCell - 1) * jumboCellWidth);
                scannedRange.minZ = Math.Min(scannedRange.minZ, (ZposOfJumboCell - 1) * jumboCellWidth);
                scannedRange.maxX = Math.Max(scannedRange.maxX, (XposOfJumboCell + 2) * jumboCellWidth - 1);
                scannedRange.maxZ = Math.Max(scannedRange.maxZ, (ZposOfJumboCell + 2) * jumboCellWidth - 1);
            }
        }

        private static IEnumerable<Thing> GetThingsAtCellCopy(int XposOfJumboCell, int ZposOfJumboCell,
            int jumboCellColumnsInMap, HashSet<Thing>[] thingsGrid)
        {
            var cellIndex = CellToIndexCustom3(XposOfJumboCell, ZposOfJumboCell, jumboCellColumnsInMap);
            if (cellIndex >= thingsGrid.Length) Log.Error("thingsGrid cellIndex");
            var thingsAtCell = thingsGrid[cellIndex];
            if (thingsAtCell != null && thingsAtCell.Count > 0)
            {
                var thingsAtCellCopy = new HashSet<Thing>(thingsAtCell);
                foreach (var thing in thingsAtCellCopy)
                    if (!returnedThings.Contains(thing))
                    {
                        yield return thing;
                        returnedThings.Add(thing);
                    }
            }
        }

        private static void AddThingToHashSets(Thing thing, ThingRequestGroup thingRequestGroup)
        {
            int jumboCellWidth;
            var map = thing.Map;
            var mapSizeX = map.Size.x;
            var mapSizeZ = map.Size.z;
            int zoomLevel;

            var thingsZoomLevels = GetZoomsFromGroup(map, thingRequestGroup);
            zoomLevel = 0;
            do
            {
                jumboCellWidth = getJumboCellWidth(zoomLevel);
                var thingsGrid = thingsZoomLevels[zoomLevel];
                var jumboCellIndex = CellToIndexCustom(thing.Position, mapSizeX, jumboCellWidth);
                lock (thingsGrid)
                {
                    var hashset = thingsGrid[jumboCellIndex];
                    if (hashset == null)
                    {
                        hashset = new HashSet<Thing>();
                        thingsGrid[jumboCellIndex] = hashset;
                    }

                    hashset.Add(thing);
                    //thingsGrid[jumboCellIndex] = newHashset;
                }

                zoomLevel++;
            } while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
        }

        private static void RemoveThingFromHashSets(Thing haulableThing, ThingRequestGroup thingRequestGroup)
        {
            int jumboCellWidth;
            var map = haulableThing.Map;
            if (map == null)
                return; //TODO not optimal
            var mapSizeX = map.Size.x;
            var mapSizeZ = map.Size.z;
            int zoomLevel;
            var thingsZoomLevels = GetZoomsFromGroup(map, thingRequestGroup);
            zoomLevel = 0;
            do
            {
                jumboCellWidth = getJumboCellWidth(zoomLevel);
                var thingsGrid = thingsZoomLevels[zoomLevel];
                var jumboCellIndex = CellToIndexCustom(haulableThing.Position, mapSizeX, jumboCellWidth);
                lock (thingsGrid)
                {
                    var hashset = thingsGrid[jumboCellIndex];
                    if (hashset != null)
                    {
                        var newHashSet = new HashSet<Thing>(hashset);
                        newHashSet.Remove(haulableThing);
                        thingsGrid[jumboCellIndex] = newHashSet;
                    }
                }

                zoomLevel++;
            } while (jumboCellWidth < mapSizeX || jumboCellWidth < mapSizeZ);
        }
    }
}