﻿using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class GenTemperature_Patch
    {
        [ThreadStatic] public static Room[] beqRooms;
        public static Dictionary<int, float> SeasonalShiftAmplitudeCache = new Dictionary<int, float>();
        public static Dictionary<int, float> tileTemperature = new Dictionary<int, float>();

        public static Dictionary<int, Dictionary<int, float>> tileAbsTickTemperature =
            new Dictionary<int, Dictionary<int, float>>();

        private static readonly Type original = typeof(GenTemperature);
        private static readonly Type patched = typeof(GenTemperature_Patch);
        private static WorldGrid worldGrid;

        public static void InitializeThreadStatics()
        {
            beqRooms = new Room[4];
        }

        public static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "GetTemperatureFromSeasonAtTile");
            RimThreadedHarmony.Prefix(original, patched, "SeasonalShiftAmplitudeAt");
        }

        public static bool SeasonalShiftAmplitudeAt(ref float __result, int tile)
        {
            var newWorldGrid = Find.WorldGrid;
            if (worldGrid != newWorldGrid)
            {
                worldGrid = newWorldGrid;
                SeasonalShiftAmplitudeCache.Clear();
                tileAbsTickTemperature.Clear();
                tileTemperature.Clear();
                if (Prefs.LogVerbose) Log.Message("RimThreaded is rebuilding WorldGrid Temperature Cache");
            }

            if (SeasonalShiftAmplitudeCache.TryGetValue(tile, out __result)) return false;
            __result = Find.WorldGrid.LongLatOf(tile).y >= 0.0
                ? TemperatureTuning.SeasonalTempVariationCurve.Evaluate(
                    newWorldGrid.DistanceFromEquatorNormalized(tile))
                : -TemperatureTuning.SeasonalTempVariationCurve.Evaluate(
                    newWorldGrid.DistanceFromEquatorNormalized(tile));
            SeasonalShiftAmplitudeCache[tile] = __result;
            return false;
        }

        public static bool GetTemperatureFromSeasonAtTile(ref float __result, int absTick, int tile)
        {
            var newWorldGrid = Find.WorldGrid;
            if (worldGrid != newWorldGrid)
            {
                worldGrid = newWorldGrid;
                SeasonalShiftAmplitudeCache.Clear();
                tileAbsTickTemperature.Clear();
                tileTemperature.Clear();
                if (Prefs.LogVerbose) Log.Message("RimThreaded is rebuilding WorldGrid Temperature Cache");
            }

            if (absTick == 0) absTick = 1;

            if (!tileAbsTickTemperature.TryGetValue(tile, out var absTickTemperature))
            {
                absTickTemperature = new Dictionary<int, float>();
                tileAbsTickTemperature[tile] = absTickTemperature;
            }

            if (!absTickTemperature.TryGetValue(absTick, out var temperature))
            {
                if (!tileTemperature.TryGetValue(tile, out var temperatureFromTile))
                {
                    temperatureFromTile = Find.WorldGrid[tile].temperature;
                    tileTemperature[tile] = temperatureFromTile;
                }

                temperature = temperatureFromTile + GenTemperature.OffsetFromSeasonCycle(absTick, tile);
                lock (absTickTemperature)
                {
                    absTickTemperature.SetOrAdd(absTick, temperature);
                }
            }

            __result = temperature;
            return false;
        }
    }
}