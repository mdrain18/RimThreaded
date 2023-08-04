using System;
using System.Reflection;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.RW_Patches
{
    public class SteadyEnvironmentEffects_Patch
    {
        public static SteadyEnvironmentEffectsStructure[] steadyEnvironmentEffectsStructures =
            new SteadyEnvironmentEffectsStructure[99];

        public static int totalSteadyEnvironmentEffectsTicks;
        public static int steadyEnvironmentEffectsTicksCompleted;
        public static int steadyEnvironmentEffectsCount;

        private static readonly MethodInfo MethodDoCellSteadyEffects =
            Method(typeof(SteadyEnvironmentEffects), nameof(SteadyEnvironmentEffects.DoCellSteadyEffects),
                new[] {typeof(IntVec3)});

        private static readonly Action<SteadyEnvironmentEffects, IntVec3> ActionDoCellSteadyEffects =
            (Action<SteadyEnvironmentEffects, IntVec3>) Delegate.CreateDelegate(
                typeof(Action<SteadyEnvironmentEffects, IntVec3>), MethodDoCellSteadyEffects);


        internal static void RunDestructivePatches()
        {
            var original = typeof(SteadyEnvironmentEffects);
            var patched = typeof(SteadyEnvironmentEffects_Patch);
            Prefix(original, patched, nameof(SteadyEnvironmentEffectsTick));
        }

        public static bool SteadyEnvironmentEffectsTick(SteadyEnvironmentEffects __instance)
        {
            var map2 = __instance.map;
            if (Find.TickManager.TicksGame % 97f == 0f && Rand.Chance(0.02f)) __instance.RollForRainFire();

            __instance.outdoorMeltAmount = __instance.MeltAmountAt(map2.mapTemperature.OutdoorTemp);
            __instance.snowRate = map2.weatherManager.SnowRate;
            __instance.rainRate = map2.weatherManager.RainRate;
            //__instance.deteriorationRate = Mathf.Lerp(1f, 5f, __instance.rainRate);
            var num = Mathf.CeilToInt(map2.Area * 0.0006f);
            var area = map2.Area;
            var ticks = Mathf.CeilToInt(area * 0.0006f);
            var index = steadyEnvironmentEffectsCount;
            steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffects = __instance;
            steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsCellsInRandomOrder =
                map2.cellsInRandomOrder;
            //int num = Mathf.CeilToInt((float)map2.Area * 0.0006f);
            steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsArea = area;
            //RimThreaded.steadyEnvironmentEffectsInstance = __instance;
            steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsCycleIndexOffset =
                ticks + __instance.cycleIndex;
            var ticks2 = Interlocked.Add(ref totalSteadyEnvironmentEffectsTicks, ticks);
            steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsTicks = ticks2;
            Interlocked.Increment(ref steadyEnvironmentEffectsCount);
            __instance.cycleIndex = (__instance.cycleIndex + ticks) % area;
            return false;
        }

        public static void SteadyEffectTick()
        {
            var steadyEnvironmentEffectsIndex = 0;
            while (true)
            {
                var ticketIndex = Interlocked.Increment(ref steadyEnvironmentEffectsTicksCompleted) - 1;
                if (ticketIndex >= totalSteadyEnvironmentEffectsTicks) return;
                var index = ticketIndex;
                while (ticketIndex >= steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex]
                           .steadyEnvironmentEffectsTicks) steadyEnvironmentEffectsIndex++;
                if (steadyEnvironmentEffectsIndex > 0)
                    index = ticketIndex - steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex - 1]
                        .steadyEnvironmentEffectsTicks;
                var cycleIndex = (steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex]
                                      .steadyEnvironmentEffectsCycleIndexOffset
                                  - index) % steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex]
                    .steadyEnvironmentEffectsArea;
                var c = steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex]
                    .steadyEnvironmentEffectsCellsInRandomOrder.Get(cycleIndex);
                try
                {
                    ActionDoCellSteadyEffects(
                        steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex].steadyEnvironmentEffects, c);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception ticking steadyEnvironmentEffectsCells " + index.ToStringSafe() + ": " + ex);
                }
            }
        }

        public struct SteadyEnvironmentEffectsStructure
        {
            public SteadyEnvironmentEffects steadyEnvironmentEffects;
            public MapCellsInRandomOrder steadyEnvironmentEffectsCellsInRandomOrder;
            public int steadyEnvironmentEffectsTicks;
            public int steadyEnvironmentEffectsArea;
            public int steadyEnvironmentEffectsCycleIndexOffset;
        }
    }
}