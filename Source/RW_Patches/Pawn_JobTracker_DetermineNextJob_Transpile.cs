﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{
    public class Pawn_JobTracker_DetermineNextJob_Transpile
    {
        public static FieldRef<Pawn_JobTracker, Pawn> jobTrackerPawn = FieldRefAccess<Pawn_JobTracker, Pawn>("pawn");

        public static IEnumerable<CodeInstruction> Postfix(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var i = 0;
            var matchFound = false;
            while (i < instructionsList.Count)
                if (i + 5 < instructionsList.Count &&
                    instructionsList[i + 5].opcode == OpCodes.Stloc_0)
                {
                    matchFound = true;
                    instructionsList[i].opcode = OpCodes.Ldsfld;
                    instructionsList[i].operand =
                        Field(typeof(Pawn_JobTracker_DetermineNextJob_Transpile), "jobTrackerPawn");
                    yield return instructionsList[i];
                    i++;
                    instructionsList[i].opcode = OpCodes.Ldarg_0;
                    instructionsList[i].operand = null;
                    yield return instructionsList[i];
                    i++;
                    instructionsList[i].opcode = OpCodes.Callvirt;
                    instructionsList[i].operand = Method(typeof(FieldRef<Pawn_JobTracker, Pawn>), "Invoke");
                    yield return instructionsList[i];
                    i++;
                    instructionsList[i].opcode = OpCodes.Ldind_Ref;
                    instructionsList[i].operand = null;
                    yield return instructionsList[i];
                    i += 2;
                }
                else
                {
                    yield return instructionsList[i];
                    i++;
                }

            if (!matchFound) Log.Error("IL code instructions not found");
        }
    }
}