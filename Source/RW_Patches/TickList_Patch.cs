using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{
    public class TickList_Patch
    {
        public static List<Thing> normalThingList;
        public static int normalThingListTicks;

        public static List<Thing> rareThingList;
        public static int rareThingListTicks;

        public static List<Thing> longThingList;
        public static int longThingListTicks;

        public static void RunNonDestructivePatches()
        {
            var original = typeof(TickList);
            var patched = typeof(TickList_Patch);
            RimThreadedHarmony.TranspileMethodLock(original, nameof(TickList.RegisterThing));
            RimThreadedHarmony.TranspileMethodLock(original, nameof(TickList.DeregisterThing));
            RimThreadedHarmony.Transpile(original, patched, nameof(Tick));
            RimThreadedHarmony.Postfix(original, patched, nameof(TickList.Tick), nameof(TickPostfix));
        }


        public static void TickPostfix(TickList __instance)
        {
            var currentTickInterval = __instance.TickInterval;
            var currentTickType = __instance.tickType;
            switch (currentTickType)
            {
                case TickerType.Normal:
                    RimThreaded.thingListNormal =
                        __instance.thingLists[Find.TickManager.TicksGame % currentTickInterval];
                    RimThreaded.thingListNormalTicks = RimThreaded.thingListNormal.Count;
                    break;
                case TickerType.Rare:
                    RimThreaded.thingListRare = __instance.thingLists[Find.TickManager.TicksGame % currentTickInterval];
                    RimThreaded.thingListRareTicks = RimThreaded.thingListRare.Count;
                    break;
                case TickerType.Long:
                    RimThreaded.thingListLong = __instance.thingLists[Find.TickManager.TicksGame % currentTickInterval];
                    RimThreaded.thingListLongTicks = RimThreaded.thingListLong.Count;
                    break;
                case TickerType.Never:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IEnumerable<CodeInstruction> Tick(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var i = 0;
            while (i < instructionsList.Count)
            {
                if (i + 2 < instructionsList.Count && instructionsList[i + 2].opcode == OpCodes.Call)
                    if (instructionsList[i + 2].operand is MethodInfo methodInfo)
                        if (methodInfo == Method(typeof(Find), "get_TickManager"))
                        {
                            var labels = instructionsList[i].labels;
                            while (i < instructionsList.Count)
                            {
                                if (instructionsList[i].opcode == OpCodes.Blt)
                                {
                                    i++;
                                    foreach (var label in labels) instructionsList[i].labels.Add(label);
                                    break;
                                }

                                i++;
                            }
                        }

                yield return instructionsList[i++];
            }
        }

        public static void NormalThingPrepare()
        {
            var tickList = RimThreaded.callingTickManager.tickListNormal;
            tickList.Tick();
            var currentTickInterval = tickList.TickInterval;
            normalThingList = tickList.thingLists[RimThreaded.callingTickManager.TicksGame % currentTickInterval];
            normalThingListTicks = normalThingList.Count;
        }

        public static void NormalThingTick()
        {
            while (true)
            {
                var index = Interlocked.Decrement(ref normalThingListTicks);
                if (index < 0) return;
                var thing = normalThingList[index];
                if (thing.Destroyed) continue;
                try
                {
                    thing.Tick();
                }
                catch (Exception ex)
                {
                    var text = thing.Spawned ? " (at " + thing.Position + ")" : "";
                    if (Prefs.DevMode)
                        Log.Error("Exception ticking " + thing.ToStringSafe() + text + ": " + ex);
                    else
                        Log.ErrorOnce(
                            "Exception ticking " + thing.ToStringSafe() + text +
                            ". Suppressing further errors. Exception: " + ex, thing.thingIDNumber ^ 0x22627165);
                }
            }
        }

        public static void RareThingPrepare()
        {
            var tickList = RimThreaded.callingTickManager.tickListRare;
            tickList.Tick();
            var currentTickInterval = tickList.TickInterval;
            rareThingList = tickList.thingLists[RimThreaded.callingTickManager.TicksGame % currentTickInterval];
            rareThingListTicks = rareThingList.Count;
        }

        public static void RareThingTick()
        {
            while (true)
            {
                var index = Interlocked.Decrement(ref rareThingListTicks);
                if (index < 0) return;
                var thing = rareThingList[index];
                if (thing.Destroyed) continue;
                try
                {
                    thing.TickRare();
                }
                catch (Exception ex)
                {
                    var text = thing.Spawned ? " (at " + thing.Position + ")" : "";
                    if (Prefs.DevMode)
                        Log.Error("Exception ticking " + thing.ToStringSafe() + text + ": " + ex);
                    else
                        Log.ErrorOnce(
                            "Exception ticking " + thing.ToStringSafe() + text +
                            ". Suppressing further errors. Exception: " + ex, thing.thingIDNumber ^ 0x22627165);
                }
            }
        }

        public static void LongThingPrepare()
        {
            var tickList = RimThreaded.callingTickManager.tickListLong;
            tickList.Tick();
            var currentTickInterval = tickList.TickInterval;
            longThingList = tickList.thingLists[RimThreaded.callingTickManager.TicksGame % currentTickInterval];
            longThingListTicks = longThingList.Count;
        }

        public static void LongThingTick()
        {
            while (true)
            {
                var index = Interlocked.Decrement(ref longThingListTicks);
                if (index < 0) return;
                var thing = longThingList[index];
                if (thing.Destroyed) continue;
                try
                {
                    thing.TickLong();
                }
                catch (Exception ex)
                {
                    var text = thing.Spawned ? " (at " + thing.Position + ")" : "";
                    if (Prefs.DevMode)
                        Log.Error("Exception ticking " + thing.ToStringSafe() + text + ": " + ex);
                    else
                        Log.ErrorOnce(
                            "Exception ticking " + thing.ToStringSafe() + text +
                            ". Suppressing further errors. Exception: " + ex, thing.thingIDNumber ^ 0x22627165);
                }
            }
        }
    }
}