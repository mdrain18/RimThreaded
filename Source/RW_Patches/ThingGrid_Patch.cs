﻿using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class ThingGrid_Patch
    {
        private static int CellToIndexCustom(IntVec3 c, int mapSizeX, int cellSize)
        {
            return (mapSizeX * c.z + c.x) / cellSize;
        }

        private static int NumGridCellsCustom(int mapSizeX, int mapSizeZ, int cellSize)
        {
            return Mathf.CeilToInt(mapSizeX * mapSizeZ / (float) cellSize);
        }

        public static void RunDestructivePatches()
        {
            var original = typeof(ThingGrid);
            var patched = typeof(ThingGrid_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(RegisterInCell));
            RimThreadedHarmony.Prefix(original, patched, nameof(DeregisterInCell));
        }

        public static bool RegisterInCell(ThingGrid __instance, Thing t, IntVec3 c)
        {
            var this_map = __instance.map;
            if (!c.InBounds(this_map))
            {
                Log.Warning(t + " tried to register out of bounds at " + c + ". Destroying.");
                t.Destroy();
            }
            else
            {
                var index = this_map.cellIndices.CellToIndex(c);

                //int mapSizeX = this_map.Size.x;
                //int mapSizeZ = this_map.Size.z;

                lock (__instance)
                {
                    //__instance.thingGrid[index].Add(t);
                    var thingGridCopy = new List<Thing>(__instance.thingGrid[index]) {t};
                    __instance.thingGrid[index] = thingGridCopy;
                }

                if (t.def.EverHaulable) HaulingCache.RegisterHaulableItem(t);
                if (!(t is Pawn || t is Mote))
                    //Log.Message(t.ToString());
                    ListerThings_Patch.RegisterListerThing(t);

                if (t is Building_PlantGrower building_PlantGrower)
                    foreach (var plantableLocation in building_PlantGrower.OccupiedRect())
                        JumboCell.ReregisterObject(t.Map, plantableLocation, RimThreaded.plantSowing_Cache);
                /*
                if (!thingBillPoints.TryGetValue(t.def, out Dictionary<WorkGiver_Scanner, float> billPointsDict))
                {
                    billPointsDict = new Dictionary<WorkGiver_Scanner, float>();
                    thingBillPoints[t.def] = billPointsDict;
                }
                if (!mapIngredientDict.TryGetValue(this_map, out Dictionary<WorkGiver_Scanner, Dictionary<float, List<HashSet<Thing>[]>>> ingredientDict))
                {
                    ingredientDict = new Dictionary<WorkGiver_Scanner, Dictionary<float, List<HashSet<Thing>[]>>>();
                    mapIngredientDict[this_map] = ingredientDict;
                }
                foreach (KeyValuePair<WorkGiver_Scanner, float> billPoints in billPointsDict)
                {
                    int i = 0;
                    int power2;
                    do
                    {
                        power2 = power2array[i];
                        ingredientDict[billPoints.Key][billPoints.Value][i][CellToIndexCustom(c, mapSizeX, power2)].Add(t);
                        i++;
                    } while (power2 < mapSizeX || power2 < mapSizeZ);
                }
                */
                //}
            }

            return false;
        }

        public static bool DeregisterInCell(ThingGrid __instance, Thing t, IntVec3 c)
        {
            var this_map = __instance.map;
            if (!c.InBounds(this_map))
            {
                Log.Error(t + " tried to de-register out of bounds at " + c);
                return false;
            }

            var index = this_map.cellIndices.CellToIndex(c);
            var thingGridInstance = __instance.thingGrid;
            var thingList = thingGridInstance[index];
            List<Thing> newThingList = null;
            if (thingList.Contains(t))
            {
                var found = false;
                lock (__instance)
                {
                    thingList = thingGridInstance[index];
                    if (thingList.Contains(t))
                    {
                        found = true;
                        newThingList = new List<Thing>(thingList);
                        newThingList.Remove(t);
                        thingGridInstance[index] = newThingList;
                    }
                }

                if (found)
                {
                    if (t.def.EverHaulable) HaulingCache.DeregisterHaulableItem(t);
                    if (!(t is Pawn || t is Mote))
                        ListerThings_Patch.DeregisterListerThing(t);

                    if (c.GetZone(__instance.map) is Zone_Growing zone)
                        JumboCell.ReregisterObject(zone.Map, c, RimThreaded.plantSowing_Cache);

                    for (var i = newThingList.Count - 1; i >= 0; i--)
                    {
                        var thing2 = newThingList[i];
                        if (thing2 is Building_PlantGrower building_PlantGrower)
                            foreach (var plantableLocation in building_PlantGrower.OccupiedRect())
                                JumboCell.ReregisterObject(building_PlantGrower.Map, plantableLocation,
                                    RimThreaded.plantSowing_Cache);
                    }
                }
                /*
                int mapSizeX = this_map.Size.x;
                int mapSizeZ = this_map.Size.z;

                if (!thingBillPoints.TryGetValue(t.def, out Dictionary<WorkGiver_Scanner, float> billPointsDict))
                {
                    billPointsDict = new Dictionary<WorkGiver_Scanner, float>();
                    thingBillPoints[t.def] = billPointsDict;
                }
                if (!mapIngredientDict.TryGetValue(this_map, out Dictionary<WorkGiver_Scanner, Dictionary<float, List<HashSet<Thing>[]>>> ingredientDict))
                {
                    ingredientDict = new Dictionary<WorkGiver_Scanner, Dictionary<float, List<HashSet<Thing>[]>>>();
                    mapIngredientDict[this_map] = ingredientDict;
                }
                foreach (KeyValuePair<WorkGiver_Scanner, float> billPoints in billPointsDict)
                {
                    int i = 0;
                    int power2;
                    do
                    {
                        power2 = power2array[i];
                        HashSet<Thing> newHashSet = new HashSet<Thing>(ingredientDict[billPoints.Key][billPoints.Value][i][CellToIndexCustom(c, mapSizeX, power2)]);
                        newHashSet.Remove(t);
                        ingredientDict[billPoints.Key][billPoints.Value][i][CellToIndexCustom(c, mapSizeX, power2)] = newHashSet;
                        i++;
                    } while (power2 < mapSizeX || power2 < mapSizeZ);
                }
                */
                //}
                //}
            }

            return false;
        }
    }
}