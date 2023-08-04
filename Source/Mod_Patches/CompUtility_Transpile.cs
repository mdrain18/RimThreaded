using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimThreaded.Mod_Patches;
using Verse;

namespace RimThreaded
{
    public class CompUtility_Transpile
    {
        public static IEnumerable<CodeInstruction> CompGuest(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var loadLockObjectType =
                typeof(Dictionary<,>).MakeGenericType(typeof(Pawn), Hospitality_Patch.hospitalityCompGuest);
            var loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld,
                    AccessTools.Field(Hospitality_Patch.hospitalityCompUtility, "guestComps"))
            };
            var searchInstructions = loadLockObjectInstructions.ListFullCopy();
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldloc_0));
            searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt,
                AccessTools.Method(loadLockObjectType, "Add")));

            var i = 0;
            var matchesFound = 0;

            while (i < instructionsList.Count)
                if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, i))
                {
                    matchesFound++;
                    foreach (var codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
                                 iLGenerator, instructionsList, i, searchInstructions.Count, loadLockObjectInstructions,
                                 loadLockObjectType))
                        yield return codeInstruction;
                    i += searchInstructions.Count;
                }
                else
                {
                    yield return instructionsList[i];
                    i++;
                }

            if (matchesFound < 1) Log.Error("IL code instructions not found");
        }

        public static IEnumerable<CodeInstruction> OnPawnRemoved(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var loadLockObjectType =
                typeof(Dictionary<,>).MakeGenericType(typeof(Pawn), Hospitality_Patch.hospitalityCompGuest);
            var loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld,
                    AccessTools.Field(Hospitality_Patch.hospitalityCompUtility, "guestComps"))
            };
            var searchInstructions = loadLockObjectInstructions.ListFullCopy();
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
            searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt,
                AccessTools.Method(loadLockObjectType, "Remove", new[] {typeof(Pawn)})));
            searchInstructions.Add(new CodeInstruction(OpCodes.Pop));

            var i = 0;
            var matchesFound = 0;

            while (i < instructionsList.Count)
                if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, i))
                {
                    matchesFound++;
                    foreach (var codeInstruction in RimThreadedHarmony.GetLockCodeInstructions(
                                 iLGenerator, instructionsList, i, searchInstructions.Count, loadLockObjectInstructions,
                                 loadLockObjectType))
                        yield return codeInstruction;
                    i += searchInstructions.Count;
                }
                else
                {
                    yield return instructionsList[i];
                    i++;
                }

            if (matchesFound < 1) Log.Error("IL code instructions not found");
        }
    }
}