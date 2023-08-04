using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using static RimThreaded.RimThreadedHarmony;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{
    public class ListerThings_Transpile
    {
        public static IEnumerable<CodeInstruction> Add(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            //---START EDIT---
            var matchesFound = new int[4];
            var dictionary_ThingDef_List_Thing = typeof(Dictionary<ThingDef, List<Thing>>);
            var list_Thing = typeof(List<Thing>);
            var list_ThingArray = typeof(List<Thing>[]);
            var listerThings = typeof(ListerThings);
            //---END EDIT---
            var instructionsList = instructions.ToList();
            var i = 0;
            while (i < instructionsList.Count)
            {
                var matchIndex = 0;
                if (
                    //---START EDIT---
                    i + 3 < instructionsList.Count &&
                    instructionsList[i + 3].opcode == OpCodes.Callvirt &&
                    (MethodInfo) instructionsList[i + 3].operand ==
                    Method(dictionary_ThingDef_List_Thing, "TryGetValue")
                    //---END EDIT---
                )
                {
                    var loadLockObjectInstructions = new List<CodeInstruction>
                    {
                        //---START EDIT---
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, Field(typeof(ListerThings), "listsByDef"))
                        //---END EDIT---
                    };

                    //---START EDIT---
                    var lockObject = iLGenerator.DeclareLocal(dictionary_ThingDef_List_Thing);
                    //---END EDIT---

                    var lockTaken = iLGenerator.DeclareLocal(typeof(bool));
                    foreach (var ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions,
                                 instructionsList[i]))
                        yield return ci;

                    while (i < instructionsList.Count)
                    {
                        if (
                            //---START EDIT---
                            instructionsList[i - 1].opcode == OpCodes.Callvirt &&
                            (MethodInfo) instructionsList[i - 1].operand ==
                            Method(dictionary_ThingDef_List_Thing, "Add")
                            //---END EDIT---
                        )
                            break;
                        yield return instructionsList[i++];
                    }

                    foreach (var ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
                        yield return ci;
                    matchesFound[matchIndex]++;
                    continue;
                }

                matchIndex++;
                if (
                    //---START EDIT---
                    i + 2 < instructionsList.Count &&
                    instructionsList[i].opcode == OpCodes.Ldloc_0 &&
                    instructionsList[i + 2].opcode == OpCodes.Callvirt &&
                    (MethodInfo) instructionsList[i + 2].operand == Method(list_Thing, "Add")
                    //---END EDIT---
                )
                {
                    var loadLockObjectInstructions = new List<CodeInstruction>
                    {
                        //---START EDIT---
                        new CodeInstruction(OpCodes.Ldloc_0)
                        //---END EDIT---
                    };

                    //---START EDIT---
                    var lockObject = iLGenerator.DeclareLocal(list_Thing);
                    //---END EDIT---

                    var lockTaken = iLGenerator.DeclareLocal(typeof(bool));
                    foreach (var ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions,
                                 instructionsList[i]))
                        yield return ci;

                    while (i < instructionsList.Count)
                    {
                        if (
                            //---START EDIT---
                            instructionsList[i - 1].opcode == OpCodes.Callvirt &&
                            (MethodInfo) instructionsList[i - 1].operand == Method(list_Thing, "Add")
                            //---END EDIT---
                        )
                            break;
                        yield return instructionsList[i++];
                    }

                    foreach (var ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
                        yield return ci;
                    matchesFound[matchIndex]++;
                    continue;
                }

                matchIndex++;
                if (
                    //---START EDIT---
                    i + 3 < instructionsList.Count &&
                    instructionsList[i + 1].opcode == OpCodes.Ldfld &&
                    (FieldInfo) instructionsList[i + 1].operand == Field(listerThings, "listsByGroup") &&
                    instructionsList[i + 3].opcode == OpCodes.Ldelem_Ref
                    //---END EDIT---
                )
                {
                    var loadLockObjectInstructions = new List<CodeInstruction>
                    {
                        //---START EDIT---
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, Field(listerThings, "listsByGroup"))
                        //---END EDIT---
                    };

                    //---START EDIT---
                    var lockObject = iLGenerator.DeclareLocal(list_ThingArray);
                    //---END EDIT---

                    var lockTaken = iLGenerator.DeclareLocal(typeof(bool));
                    foreach (var ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions,
                                 instructionsList[i]))
                        yield return ci;

                    while (i < instructionsList.Count)
                    {
                        if (
                            //---START EDIT---
                            instructionsList[i - 1].opcode == OpCodes.Stelem_Ref
                            //---END EDIT---
                        )
                            break;
                        yield return instructionsList[i++];
                    }

                    foreach (var ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
                        yield return ci;
                    matchesFound[matchIndex]++;
                    continue;
                }

                matchIndex++;
                if (
                    //---START EDIT---
                    i + 2 < instructionsList.Count &&
                    instructionsList[i].opcode == OpCodes.Ldloc_S &&
                    ((LocalBuilder) instructionsList[i].operand).LocalIndex == 4 &&
                    instructionsList[i + 2].opcode == OpCodes.Callvirt &&
                    (MethodInfo) instructionsList[i + 2].operand == Method(list_Thing, "Add")
                    //---END EDIT---
                )
                {
                    var loadLockObjectInstructions = new List<CodeInstruction>
                    {
                        //---START EDIT---
                        new CodeInstruction(OpCodes.Ldloc_S, 4)
                        //---END EDIT---
                    };

                    //---START EDIT---
                    var lockObject = iLGenerator.DeclareLocal(list_Thing);
                    //---END EDIT---

                    var lockTaken = iLGenerator.DeclareLocal(typeof(bool));
                    foreach (var ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions,
                                 instructionsList[i]))
                        yield return ci;

                    while (i < instructionsList.Count)
                    {
                        if (
                            //---START EDIT---
                            instructionsList[i - 1].opcode == OpCodes.Callvirt &&
                            (MethodInfo) instructionsList[i - 1].operand == Method(list_Thing, "Add")
                            //---END EDIT---
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

        public static IEnumerable<CodeInstruction> Remove(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();

            var loadLockObjectType = typeof(List<Thing>);
            var loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(ListerThings), "listsByDef")),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(Thing), "def")),
                new CodeInstruction(OpCodes.Callvirt, Method(typeof(Dictionary<ThingDef, List<Thing>>), "get_Item"))
            };
            var searchInstructions = loadLockObjectInstructions.ListFullCopy();
            searchInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
            searchInstructions.Add(new CodeInstruction(OpCodes.Callvirt, Method(typeof(Thing), "Remove")));
            searchInstructions.Add(new CodeInstruction(OpCodes.Pop));

            var loadLockObjectType2 = typeof(List<Thing>);
            var loadLockObjectInstructions2 = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(ListerThings), "listsByGroup")),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldelem_Ref)
            };
            var searchInstructions2 = loadLockObjectInstructions2.ListFullCopy();
            searchInstructions2.Add(new CodeInstruction(OpCodes.Ldarg_1));
            searchInstructions2.Add(new CodeInstruction(OpCodes.Callvirt, Method(loadLockObjectType2, "Remove")));
            searchInstructions2.Add(new CodeInstruction(OpCodes.Pop));

            var i = 0;
            var matchesFound = 0;

            while (i < instructionsList.Count)
                if (IsCodeInstructionsMatching(searchInstructions, instructionsList, i))
                {
                    matchesFound++;
                    foreach (var codeInstruction in GetLockCodeInstructions(
                                 iLGenerator, instructionsList, i, searchInstructions.Count, loadLockObjectInstructions,
                                 loadLockObjectType))
                        yield return codeInstruction;
                    i += searchInstructions.Count;
                }
                else if (IsCodeInstructionsMatching(searchInstructions2, instructionsList, i))
                {
                    matchesFound++;
                    foreach (var codeInstruction in GetLockCodeInstructions(
                                 iLGenerator, instructionsList, i, searchInstructions2.Count,
                                 loadLockObjectInstructions2, loadLockObjectType2))
                        yield return codeInstruction;
                    i += searchInstructions2.Count;
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