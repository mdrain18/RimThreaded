using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.Mod_Patches.CombatExteneded_Patch;

namespace RimThreaded.Mod_Patches
{
    internal class ProjectileCE_Transpile
    {
        private static readonly MethodInfo methodTryCollideWith =
            Method(combatExtended_ProjectileCE, "dTryCollideWith", new[] {typeof(Thing)});

        private static readonly Func<object, Thing, bool> funcTryCollideWith =
            (Func<object, Thing, bool>) Delegate.CreateDelegate(typeof(Func<object, Thing, bool>),
                methodTryCollideWith);

        private static readonly MethodInfo methodApplySuppression =
            Method(typeof(LongEventHandler), "ApplySuppression", new[] {typeof(Pawn)});

        private static readonly Action<Pawn> actionApplySuppression =
            (Action<Pawn>) Delegate.CreateDelegate(typeof(Action<Pawn>), methodApplySuppression);

        public static List<Thing> CheckCellForCollision2(List<Thing> thingsListAtFast)
        {
            //List<Thing> list = new List<Thing>(map.thingGrid.ThingsListAtFast(cell)).Where((Thing t) => justWallsRoofs ? (t.def.Fillage == FillCategory.Full) : (t is Pawn || t.def.Fillage != FillCategory.None)).ToList();
            var returnList = new List<Thing>();
            for (var i = 0; i < thingsListAtFast.Count; i++)
            {
                Thing t;
                try
                {
                    t = thingsListAtFast[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }

                if (t is Pawn || t.def.Fillage != FillCategory.None) returnList.Add(t);
            }

            return returnList;
        }

        public static bool CheckCellForCollision3(object projectileCE, List<Thing> list, Vector3 LastPos,
            Thing launcher, Thing mount, bool canTargetSelf,
            bool flag2, Thing intendedTarget, Vector3 ExactPosition)
        {
            foreach (var item2 in from x in list.Distinct()
                     orderby (x.DrawPos - LastPos).sqrMagnitude
                     select x)
            {
                if ((item2 == launcher || item2 == mount) && !canTargetSelf) continue;
                if ((!flag2 || item2 == intendedTarget) && funcTryCollideWith(projectileCE, item2)) return true;
                //if (justWallsRoofs)
                //{
                //continue;
                //}
                var exactPosition = ExactPosition;
                if (exactPosition.y < 3f)
                {
                    var pawn = item2 as Pawn;
                    if (pawn != null) actionApplySuppression(pawn);
                }
            }

            return false;
        }

        public static bool CheckForCollisionBetween2(IOrderedEnumerable<IntVec3> orderedEnumerable)
        {
            //foreach (IntVec3 intVec3 in orderedEnumerable)
            //for(int i = 0; i < orderedEnumerable.Count)
            //{
            //if (this.CheckCellForCollision(intVec3))
            //{
            //return true;
            //}
            /*
            if (Controller.settings.DebugDrawInterceptChecks)
            {
                base.Map.debugDrawer.FlashCell(intVec3, 1f, "o", 50);
            }
            */
            //}
            return false;
        }

        public static IEnumerable<CodeInstruction> CheckCellForCollision(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var matchesFound = new int[1]; //EDIT
            var instructionsList = instructions.ToList();
            var i = 0;
            while (i < instructionsList.Count)
            {
                var matchIndex = 0;
                if (
                    instructionsList[i].opcode == OpCodes.Newobj && //EDIT
                    (ConstructorInfo) instructionsList[i].operand ==
                    Constructor(typeof(List<Thing>), new[] {typeof(IEnumerable<Thing>)}) //EDIT
                )
                {
                    instructionsList[i].opcode = OpCodes.Call;
                    instructionsList[i].operand = Method(typeof(ProjectileCE_Transpile), "CheckCellForCollision2");
                    while (i < instructionsList.Count)
                    {
                        if (
                            instructionsList[i].opcode == OpCodes.Stloc_S && //EDIT
                            ((LocalBuilder) instructionsList[i].operand).LocalIndex == 4 //EDIT
                        )
                            break;
                        i++;
                    }

                    matchesFound[matchIndex]++;
                    continue;
                }

                yield return instructionsList[i++];
            }

            for (var mIndex = 0; mIndex < matchesFound.Length; mIndex++)
                if (matchesFound[mIndex] < 1)
                    Log.Error("IL code instruction set " + mIndex + " not found");
        }


        public static IEnumerable<CodeInstruction> CheckForCollisionBetween(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var matchesFound = new int[1]; //EDIT
            var instructionsList = instructions.ToList();
            var i = 0;
            while (i < instructionsList.Count)
            {
                var matchIndex = 0;
                if (
                    instructionsList[i].opcode == OpCodes.Newobj && //EDIT
                    (ConstructorInfo) instructionsList[i].operand ==
                    Constructor(typeof(List<Thing>), new[] {typeof(IEnumerable<Thing>)}) //EDIT
                )
                {
                    instructionsList[i].opcode = OpCodes.Call;
                    instructionsList[i].operand = Method(typeof(ProjectileCE_Transpile), "CheckCellForCollision2");
                    while (i < instructionsList.Count)
                    {
                        if (
                            instructionsList[i].opcode == OpCodes.Stloc_S && //EDIT
                            ((LocalBuilder) instructionsList[i].operand).LocalIndex == 4 //EDIT
                        )
                            break;
                        i++;
                    }

                    matchesFound[matchIndex]++;
                    continue;
                }

                yield return instructionsList[i++];
            }

            for (var mIndex = 0; mIndex < matchesFound.Length; mIndex++)
                if (matchesFound[mIndex] < 1)
                    Log.Error("IL code instruction set " + mIndex + " not found");
        }
    }
}