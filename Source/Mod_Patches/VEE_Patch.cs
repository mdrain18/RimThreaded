﻿using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    internal class VEE_Patch
    {
        public static void Patch()
        {
            var VEE_FertilityGrid = TypeByName("VEE.FertilityGrid_Patch");
            if (VEE_FertilityGrid != null)
            {
                var methodName = nameof(Postfix);
                Log.Message("RimThreaded is patching " + VEE_FertilityGrid.FullName + " " + methodName);
                Transpile(VEE_FertilityGrid, typeof(VEE_Patch), methodName);
            }

            var VEE_Plant_GrowthRate = TypeByName("VEE.Plant_GrowthRate_Patch");
            if (VEE_Plant_GrowthRate != null)
            {
                var methodName = nameof(Postfix);
                Log.Message("RimThreaded is patching " + VEE_Plant_GrowthRate.FullName + " " + methodName);
                Transpile(VEE_Plant_GrowthRate, typeof(VEE_Patch), methodName);
            }

            var VEE_Plant_TickLong = TypeByName("VEE.Plant_TickLong_Patch");
            if (VEE_Plant_TickLong != null)
            {
                var methodName = nameof(Postfix);
                Log.Message("RimThreaded is patching " + VEE_Plant_TickLong.FullName + " " + methodName);
                Transpile(VEE_Plant_TickLong, typeof(VEE_Patch), methodName);
            }

            var MapComp_Drought = TypeByName("VEE.MapComp_Drought");
            if (MapComp_Drought != null)
            {
                var methodName = nameof(MapComponentTick);
                Log.Message("RimThreaded is patching " + MapComp_Drought.FullName + " " + methodName);
                Transpile(MapComp_Drought, typeof(VEE_Patch), methodName);
            }
        }

        public static bool ContainsKey(Dictionary<Map, object> MapComp_Drought, Map m)
        {
            lock (MapComp_Drought)
            {
                return MapComp_Drought.ContainsKey(m);
            }
        }

        public static bool ContainsKey2(Dictionary<Plant, bool> affectedPlants, Plant p)
        {
            lock (affectedPlants)
            {
                return affectedPlants.ContainsKey(p);
            }
        }

        public static void SetOrAdd(Dictionary<Plant, bool> affectedPlants, Plant p, bool b) //extension method
        {
            lock (affectedPlants)
            {
                affectedPlants[p] = b;
            }
        }

        public static void Add(Dictionary<Map, object> MapComp_Drought, Map m, object j)
        {
            lock (MapComp_Drought)
            {
                MapComp_Drought[m] = j;
            }
        }

        public static object
            TryGetValue(Dictionary<Map, object> MapComp_Drought, Map m, object o = null) //extension method
        {
            lock (MapComp_Drought)
            {
                if (MapComp_Drought.ContainsKey(m)) return MapComp_Drought[m];
                return o;
            }
        }

        public static IEnumerable<CodeInstruction> Postfix(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var MapComp_Drought = typeof(Dictionary<,>).MakeGenericType(typeof(Map), TypeByName("VEE.MapComp_Drought"));
            var affectedPlants = typeof(Dictionary<Plant, bool>);
            var GenCollection = typeof(GenCollection); // You need to refer the extension class
            foreach (var i in instructions)
            {
                if (i.opcode == OpCodes.Callvirt || i.opcode == OpCodes.Call)
                {
                    if ((MethodInfo) i.operand == Method(MapComp_Drought, "ContainsKey"))
                        i.operand = Method(typeof(VEE_Patch), nameof(ContainsKey));
                    if ((MethodInfo) i.operand == Method(affectedPlants, "ContainsKey"))
                        i.operand = Method(typeof(VEE_Patch), nameof(ContainsKey2));
                    if ((MethodInfo) i.operand ==
                        Method(GenCollection, "SetOrAdd", null, new[] {typeof(Plant), typeof(bool)}))
                        i.operand = Method(typeof(VEE_Patch), nameof(SetOrAdd));
                    if ((MethodInfo) i.operand == Method(MapComp_Drought, "Add"))
                        i.operand = Method(typeof(VEE_Patch), nameof(Add));
                    if ((MethodInfo) i.operand == Method(GenCollection, "TryGetValue", null,
                            new[] {typeof(Map), TypeByName("VEE.MapComp_Drought")}))
                        i.operand = Method(typeof(VEE_Patch), nameof(TryGetValue));
                }

                yield return i;
            }
        }

        public static void Clear(Dictionary<Plant, bool> affectedPlants)
        {
            lock (affectedPlants)
            {
                affectedPlants.Clear();
            }
        }

        public static IEnumerable<CodeInstruction> MapComponentTick(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var affectedPlants = typeof(Dictionary<Plant, bool>);
            foreach (var i in instructions)
            {
                if (i.opcode == OpCodes.Callvirt)
                    if ((MethodInfo) i.operand == Method(affectedPlants, "Clear"))
                        i.operand = Method(typeof(VEE_Patch), nameof(Clear));
                yield return i;
            }
        }
    }
}