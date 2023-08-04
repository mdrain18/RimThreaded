using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.RW_Patches
{
    internal class Hauling_Transpile
    {
        public static IEnumerable<CodeInstruction> CanHaul(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var matchesFound = new int[1]; //EDIT
            var dictionary_Thing_IntVec3 = typeof(Dictionary<Thing, IntVec3>); //EDIT
            var instructionsList = instructions.ToList();
            var i = 0;
            while (i < instructionsList.Count)
            {
                var matchIndex = 0;
                if (
                    i + 5 < instructionsList.Count && //EDIT
                    instructionsList[i].opcode == OpCodes.Ldsfld && //EDIT
                    (FieldInfo) instructionsList[i].operand == cachedStoreCell && //EDIT
                    instructionsList[i + 5].opcode == OpCodes.Call //EDIT
                )
                {
                    var loadLockObjectInstructions = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldsfld, cachedStoreCell) //EDIT
                    };
                    var lockObject = iLGenerator.DeclareLocal(dictionary_Thing_IntVec3); //EDIT
                    var lockTaken = iLGenerator.DeclareLocal(typeof(bool));
                    foreach (var ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions,
                                 instructionsList[i]))
                        yield return ci;

                    while (i < instructionsList.Count)
                    {
                        if (
                            instructionsList[i - 1].opcode == OpCodes.Call //EDIT
                        )
                            break;
                        yield return instructionsList[i++];
                    }

                    foreach (var ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
                        yield return ci;
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