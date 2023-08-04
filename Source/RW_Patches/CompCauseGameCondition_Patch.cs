using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class CompCauseGameCondition_Patch
    {
        [ThreadStatic] public static List<Map> tmpDeadConditionMaps;

        internal static void RunDestructivePatches()
        {
            var original = typeof(CompCauseGameCondition);
            var patched = typeof(CompCauseGameCondition_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(GetConditionInstance));
            RimThreadedHarmony.Prefix(original, patched, nameof(CreateConditionOn));
            RimThreadedHarmony.Prefix(original, patched, nameof(CompTick));
        }

        public static void InitializeThreadStatics()
        {
            tmpDeadConditionMaps = new List<Map>();
        }

        public static bool GetConditionInstance(CompCauseGameCondition __instance, ref GameCondition __result, Map map)
        {
            if (!__instance.causedConditions.TryGetValue(map, out var value) &&
                __instance.Props.preventConditionStacking)
            {
                value = map.GameConditionManager.GetActiveCondition(__instance.Props.conditionDef);
                if (value != null)
                {
                    lock (__instance)
                    {
                        __instance.causedConditions.Add(map, value);
                    }

                    __instance.SetupCondition(value, map);
                }
            }

            __result = value;
            return false;
        }

        public static bool CreateConditionOn(CompCauseGameCondition __instance, ref GameCondition __result, Map map)
        {
            var gameCondition = GameConditionMaker.MakeCondition(__instance.ConditionDef);
            gameCondition.Duration = gameCondition.TransitionTicks;
            gameCondition.conditionCauser = __instance.parent;
            map.gameConditionManager.RegisterCondition(gameCondition);
            lock (__instance)
            {
                __instance.causedConditions.Add(map, gameCondition);
            }

            __instance.SetupCondition(gameCondition, map);
            __result = gameCondition;
            return false;
        }

        public static bool CompTick(CompCauseGameCondition __instance)
        {
            if (__instance.Active)
                foreach (var map in Find.Maps)
                    if (__instance.InAoE(map.Tile))
                        __instance.EnforceConditionOn(map);
            tmpDeadConditionMaps.Clear();

            foreach (var causedCondition in __instance.causedConditions)
                if (causedCondition.Value.Expired ||
                    !causedCondition.Key.GameConditionManager.ConditionIsActive(causedCondition.Value.def))
                    tmpDeadConditionMaps.Add(causedCondition.Key);

            foreach (var tmpDeadConditionMap in tmpDeadConditionMaps)
            {
                if (!__instance.causedConditions.ContainsKey(tmpDeadConditionMap)) continue;
                lock (__instance)
                {
                    if (!__instance.causedConditions.ContainsKey(tmpDeadConditionMap)) continue;
                    var newCausedConditions = new Dictionary<Map, GameCondition>(__instance.causedConditions);
                    newCausedConditions.Remove(tmpDeadConditionMap);
                    __instance.causedConditions = newCausedConditions;
                }
            }

            return false;
        }
    }
}