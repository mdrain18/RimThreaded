using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{
    internal class Patch_TryOpportunisticJob_Transpile
    {
        public static IEnumerable<CodeInstruction> TryOpportunisticJob(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var matchesFound = new int[1];
            var instructionsList = instructions.ToList();
            var i = 0;
            while (i < instructionsList.Count)
            {
                var matchIndex = 0;
                if (
                    i + 3 < instructionsList.Count &&
                    instructionsList[i + 3].opcode == OpCodes.Callvirt &&
                    instructionsList[i + 3].operand.ToString().Contains("GetValue")
                )
                {
                    matchesFound[matchIndex]++;
                    instructionsList[i].opcode = OpCodes.Call;
                    instructionsList[i].operand = Method(typeof(Patch_TryOpportunisticJob), "getPawn");
                    yield return instructionsList[i++];
                    i += 3;
                }

                yield return instructionsList[i++];
            }

            for (var mIndex = 0; mIndex < matchesFound.Length; mIndex++)
                if (matchesFound[mIndex] < 1)
                    Log.Error("IL code instruction set " + mIndex + " not found");
        }
    }
}