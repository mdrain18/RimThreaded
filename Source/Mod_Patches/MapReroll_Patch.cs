﻿using System;
using System.Collections.Generic;
using RimThreaded.RW_Patches;
using RimWorld.Planet;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    internal class MapReroll_Patch
    {
        public static Type MapReroll;

        public static void Patch()
        {
            MapReroll = TypeByName("MapReroll.MapPreviewGenerator");
            if (MapReroll != null)
            {
                Log.Message("RimThreaded is patching methods for compatibility with MapReroll");
                var original = typeof(World);
                var patched = typeof(MapReroll_Patch);
                Prefix(original, patched, nameof(NaturalRockTypesIn));
                Prefix(original, patched, nameof(CoastDirectionAt));
            }
        }

        public static bool NaturalRockTypesIn(World __instance, ref IEnumerable<ThingDef> __result, int tile)
        {
            if (World_Patch.tmpNaturalRockDefs == null) World_Patch.tmpNaturalRockDefs = new List<ThingDef>();
            return true;
        }

        public static bool CoastDirectionAt(World __instance, ref Rot4 __result, int tileID)
        {
            if (World_Patch.tmpOceanDirs == null) World_Patch.tmpOceanDirs = new List<Rot4>();
            if (World_Patch.tmpNeighbors == null) World_Patch.tmpNeighbors = new List<int>();
            return true;
        }
    }
}