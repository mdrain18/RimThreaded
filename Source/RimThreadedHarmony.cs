﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Newtonsoft.Json;
using RimThreaded.Mod_Patches;
using RimThreaded.RW_Patches;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class RimThreadedHarmony
    {
        public static Harmony harmony = new Harmony("majorhoff.rimthreaded");

        public static FieldInfo cachedStoreCell;
        public static HashSet<MethodInfo> nonDestructivePrefixes = new HashSet<MethodInfo>();
        public static List<Assembly> assemblies;

        private static Replacements replacements;

        public static Dictionary<Type, HashSet<FieldInfo>> untouchedStaticFields =
            new Dictionary<Type, HashSet<FieldInfo>>();

        public static HashSet<string> fieldFullNames = new HashSet<string>();
        public static HashSet<string> allStaticFieldNames = new HashSet<string>();

        public static readonly Dictionary<object, object> replaceFields = new Dictionary<object, object>();

        public static HashSet<object> notifiedObjects = new HashSet<object>();


        public static readonly HarmonyMethod replaceFieldsHarmonyTranspiler =
            new HarmonyMethod(Method(typeof(RimThreadedHarmony), nameof(ReplaceFieldsTranspiler)));

        public static readonly HarmonyMethod methodLockTranspiler =
            new HarmonyMethod(Method(typeof(RimThreadedHarmony), nameof(WrapMethodInInstanceLock)));

        public static readonly HarmonyMethod add3Transpiler =
            new HarmonyMethod(Method(typeof(RimThreadedHarmony), nameof(Add3Transpiler)));

        static RimThreadedHarmony()
        {
            if (Prefs.LogVerbose) Harmony.DEBUG = true;
            Log.Message("RimThreaded " + Assembly.GetExecutingAssembly().GetName().Version +
                        "  is patching methods...");

            LoadFieldReplacements();
            AddAdditionalReplacements();
            ApplyFieldReplacements();
            PatchDestructiveFixes();
            PatchNonDestructiveFixes();
            PatchModCompatibility();

            Log.Message("RimThreaded patching is complete.");

            var potentialConflicts = RimThreadedMod.GetPotentialModConflicts();
            if (potentialConflicts.Length > 0)
                Log.Warning("Potential RimThreaded mod conflicts :" + potentialConflicts);
        }

        private static void AddAdditionalReplacements()
        {
            SimplePool_Patch_RunNonDestructivePatches();
            GraphicDatabase_Patch.RunNonDestructivePatches();
            FullPool_Patch_RunNonDestructivePatches();
            Dijkstra_Patch_RunNonDestructivePatches();
            PathFinder_Patch.AddFieldReplacements();
            Region_Patch.AddFieldReplacements();
            replaceFields.Add(Method(typeof(Time), "get_realtimeSinceStartup"),
                Method(typeof(Time_Patch), nameof(Time_Patch.get_realtimeSinceStartup)));
            replaceFields.Add(Method(typeof(RenderTexture), nameof(RenderTexture.ReleaseTemporary)),
                Method(typeof(RenderTexture_Patch), nameof(RenderTexture_Patch.ReleaseTemporary)));
            //replaceFields.Add(Method(typeof(AudioLowPassFilter), "set_cutoffFrequency"), Method(typeof(AudioLowPassFilter_Patch), "set_cutoffFrequency"));
            //replaceFields.Add(Method(typeof(AudioLowPassFilter), "set_lowpassResonanceQ"), Method(typeof(AudioLowPassFilter_Patch), "set_lowpassResonanceQ"));
            //replaceFields.Add(Method(typeof(AudioHighPassFilter), "set_cutoffFrequency"), Method(typeof(AudioHighPassFilter_Patch), "set_cutoffFrequency"));
            //replaceFields.Add(Method(typeof(AudioHighPassFilter), "set_highpassResonanceQ"), Method(typeof(AudioHighPassFilter_Patch), "set_highpassResonanceQ"));
            replaceFields.Add(
                Method(typeof(MeshMakerPlanes), "NewPlaneMesh",
                    new[] {typeof(Vector2), typeof(bool), typeof(bool), typeof(bool)}),
                Method(typeof(MeshMakerPlanes_Patch), "NewPlaneMesh"));
        }

        private static void LoadFieldReplacements()
        {
            assemblies = (from a in AppDomain.CurrentDomain.GetAssemblies()
                where !a.FullName.StartsWith("Microsoft.VisualStudio")
                select a).ToList();

            var jsonString = File.ReadAllText(RimThreadedMod.replacementsJsonPath);
            replacements = JsonConvert.DeserializeObject<Replacements>(jsonString);
            var initializer = Method(typeof(RimThreaded), nameof(RimThreaded.InitializeAllThreadStatics));
            var threadStaticConstructor = typeof(ThreadStaticAttribute).GetConstructor(new Type[0]);
            var attributeBuilder = new CustomAttributeBuilder(threadStaticConstructor, new object[0]);
            var aName = new AssemblyName("RimThreadedReplacements");
            var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
            var modBuilder = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
            foreach (var classReplacement in replacements.ClassReplacements)
            {
                var type = TypeByName(classReplacement.ClassName);
                if (type == null)
                {
                    if (!classReplacement.IgnoreMissing)
                        Log.Error("Cannot find class named: " + classReplacement.ClassName);
                    continue;
                }

                if (classReplacement.ThreadStatics != null && classReplacement.ThreadStatics.Count > 0)
                {
                    var tb = modBuilder.DefineType(type.Name + "_Replacement", TypeAttributes.Public);
                    var mb = tb.DefineMethod("InitializeThreadStatics",
                        MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
                    var il = mb.GetILGenerator();
                    foreach (var threadStaticDetail in classReplacement.ThreadStatics)
                    {
                        var fieldInfo = Field(type, threadStaticDetail.FieldName);
                        if (fieldInfo == null)
                        {
                            Log.Error("Cannot find field named: " + classReplacement.ClassName + "." +
                                      threadStaticDetail.FieldName);
                            continue;
                        }

                        FieldInfo replacementField;
                        if (threadStaticDetail.PatchedClassName == null)
                        {
                            var fb = tb.DefineField(fieldInfo.Name, fieldInfo.FieldType,
                                FieldAttributes.Public | FieldAttributes.Static);
                            fb.SetCustomAttribute(attributeBuilder);
                            replacementField = fb;
                        }
                        else
                        {
                            var replacementType = TypeByName(threadStaticDetail.PatchedClassName);
                            if (replacementType == null)
                            {
                                Log.Error("Cannot find replacement class named: " +
                                          threadStaticDetail.PatchedClassName);
                                continue;
                            }

                            replacementField = Field(replacementType, threadStaticDetail.FieldName);
                            if (replacementField == null)
                            {
                                Log.Error("Cannot find replacement field named: " +
                                          threadStaticDetail.PatchedClassName + "." + threadStaticDetail.FieldName);
                                continue;
                            }
                        }

                        replaceFields[fieldInfo] = replacementField;
                        if (!threadStaticDetail.SelfInitialized)
                        {
                            var constructor = fieldInfo.FieldType.GetConstructor(Type.EmptyTypes);
                            if (constructor != null)
                            {
                                il.Emit(OpCodes.Newobj, constructor);
                                il.Emit(OpCodes.Stsfld, replacementField);
                            }
                        }
                    }

                    il.Emit(OpCodes.Ret);
                    var newFieldType = tb.CreateType();
                    var mb2 = Method(newFieldType, "InitializeThreadStatics");
                    var pf = new HarmonyMethod(mb2);
                    harmony.Patch(initializer, postfix: pf);
                }
            }

            if (Prefs.LogVerbose) ab.Save(aName.Name + ".dll");
        }

        private static void ApplyFieldReplacements()
        {
            var MethodsFromCache = new List<MethodBase>();
            var AssembliesToPatch = new HashSet<string>
            {
                "Assembly-CSharp.dll",
                "VFECore.dll",
                "GiddyUpCore.dll",
                "SpeakUp.dll"
            };
            foreach (var assembly in assemblies)
                if (AssembliesToPatch.Contains(assembly.ManifestModule.ScopeName))
                {
                    if (Prefs.LogVerbose)
                        Log.Message("RimThreaded is attempting to load replacements from cache for assembly: " +
                                    assembly.FullName);
                    if (AssemblyCache.TryGetFromCache(assembly.ManifestModule.ModuleVersionId.ToString(),
                            out MethodsFromCache))
                    {
                        foreach (var method in MethodsFromCache) TranspileFieldReplacements(method);
                    }
                    else
                    {
                        var types = GetTypesFromAssembly(assembly);
                        foreach (var type in types)
                        {
                            var allMethods = new List<MethodBase>();
                            allMethods.AddRange(type.GetMethods(all | BindingFlags.DeclaredOnly));
                            allMethods.AddRange(type.GetConstructors(all | BindingFlags.DeclaredOnly));

                            foreach (var method in allMethods)
                                if (method.IsDeclaredMember())
                                    try
                                    {
                                        var codeInstructions = PatchProcessor.ReadMethodBody(method);
                                        foreach (var codeInstruction in codeInstructions)
                                        {
                                            if (codeInstruction.Value is FieldInfo fieldInfo &&
                                                replaceFields.ContainsKey(fieldInfo))
                                            {
                                                AssemblyCache.AddToCache(
                                                    assembly.ManifestModule.ModuleVersionId.ToString(), method, type);
                                                TranspileFieldReplacements(method);
                                                break;
                                            }

                                            if (codeInstruction.Value is MethodInfo methodInfo &&
                                                replaceFields.ContainsKey(methodInfo))
                                            {
                                                AssemblyCache.AddToCache(
                                                    assembly.ManifestModule.ModuleVersionId.ToString(), method, type);
                                                TranspileFieldReplacements(method);
                                                break;
                                            }
                                        }
                                    }
                                    // Todo: need to do something with these exceptions
                                    catch (NotSupportedException)
                                    {
                                    }
                                    catch (TypeInitializationException)
                                    {
                                    }
                                    catch (Exception)
                                    {
                                    }
                        }

                        AssemblyCache.SaveJson();
                    }
                }

            Log.Message("RimThreaded Field Replacements Complete.");
        }


        public static List<CodeInstruction> EnterLock(LocalBuilder lockObject, LocalBuilder lockTaken,
            List<CodeInstruction> loadLockObjectInstructions, CodeInstruction currentInstruction)
        {
            var codeInstructions = new List<CodeInstruction>();
            loadLockObjectInstructions[0].labels = currentInstruction.labels;
            for (var i = 0; i < loadLockObjectInstructions.Count; i++)
                codeInstructions.Add(loadLockObjectInstructions[i]);
            currentInstruction.labels = new List<Label>();
            codeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockObject.LocalIndex));
            codeInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
            codeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex));
            var codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex);
            codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            codeInstructions.Add(codeInstruction);
            codeInstructions.Add(new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex));
            codeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Enter",
                new[] {typeof(object), typeof(bool).MakeByRefType()})));
            return codeInstructions;
        }

        public static List<CodeInstruction> ExitLock(ILGenerator iLGenerator, LocalBuilder lockObject,
            LocalBuilder lockTaken, CodeInstruction currentInstruction)
        {
            var codeInstructions = new List<CodeInstruction>();
            var endHandlerDestination = iLGenerator.DefineLabel();
            codeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, endHandlerDestination));
            var codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockTaken.LocalIndex);
            codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
            codeInstructions.Add(codeInstruction);
            var endFinallyDestination = iLGenerator.DefineLabel();
            codeInstructions.Add(new CodeInstruction(OpCodes.Brfalse_S, endFinallyDestination));
            codeInstructions.Add(new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex));
            codeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Exit")));
            codeInstruction = new CodeInstruction(OpCodes.Endfinally);
            codeInstruction.labels.Add(endFinallyDestination);
            codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            codeInstructions.Add(codeInstruction);
            currentInstruction.labels.Add(endHandlerDestination);
            return codeInstructions;
        }

        public static List<CodeInstruction> GetLockCodeInstructions(
            ILGenerator iLGenerator, List<CodeInstruction> instructionsList, int currentInstructionIndex,
            int searchInstructionsCount, List<CodeInstruction> loadLockObjectInstructions,
            LocalBuilder lockObject, LocalBuilder lockTaken)
        {
            var finalCodeInstructions = new List<CodeInstruction>();
            loadLockObjectInstructions[0].labels = instructionsList[currentInstructionIndex].labels;
            for (var i = 0; i < loadLockObjectInstructions.Count; i++)
                finalCodeInstructions.Add(loadLockObjectInstructions[i]);
            instructionsList[currentInstructionIndex].labels = new List<Label>();
            finalCodeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockObject.LocalIndex));
            finalCodeInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
            finalCodeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex));
            var codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex);
            codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            finalCodeInstructions.Add(codeInstruction);
            finalCodeInstructions.Add(new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex));
            finalCodeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Enter",
                new[] {typeof(object), typeof(bool).MakeByRefType()})));
            for (var i = 0; i < searchInstructionsCount; i++)
            {
                finalCodeInstructions.Add(instructionsList[currentInstructionIndex]);
                currentInstructionIndex++;
            }

            var endHandlerDestination = iLGenerator.DefineLabel();
            finalCodeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, endHandlerDestination));
            codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockTaken.LocalIndex);
            codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
            finalCodeInstructions.Add(codeInstruction);
            var endFinallyDestination = iLGenerator.DefineLabel();
            finalCodeInstructions.Add(new CodeInstruction(OpCodes.Brfalse_S, endFinallyDestination));
            finalCodeInstructions.Add(new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex));
            finalCodeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Exit")));
            codeInstruction = new CodeInstruction(OpCodes.Endfinally);
            codeInstruction.labels.Add(endFinallyDestination);
            codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            finalCodeInstructions.Add(codeInstruction);
            instructionsList[currentInstructionIndex].labels.Add(endHandlerDestination);
            return finalCodeInstructions;
        }

        public static List<CodeInstruction> GetLockCodeInstructions(
            ILGenerator iLGenerator, List<CodeInstruction> instructionsList, int currentInstructionIndex,
            int searchInstructionsCount, List<CodeInstruction> loadLockObjectInstructions, Type lockObjectType)
        {
            var lockObject = iLGenerator.DeclareLocal(lockObjectType);
            var lockTaken = iLGenerator.DeclareLocal(typeof(bool));
            return GetLockCodeInstructions(iLGenerator, instructionsList, currentInstructionIndex,
                searchInstructionsCount, loadLockObjectInstructions, lockObject, lockTaken);
        }

        public static bool IsCodeInstructionsMatching(List<CodeInstruction> searchInstructions,
            List<CodeInstruction> instructionsList, int instructionIndex)
        {
            var instructionsMatch = false;
            if (instructionIndex + searchInstructions.Count < instructionsList.Count)
            {
                instructionsMatch = true;
                for (var searchIndex = 0; searchIndex < searchInstructions.Count; searchIndex++)
                {
                    var searchInstruction = searchInstructions[searchIndex];
                    var originalInstruction = instructionsList[instructionIndex + searchIndex];
                    var searchOperand = searchInstruction.operand;
                    var orginalOperand = originalInstruction.operand;
                    if (searchInstruction.opcode != originalInstruction.opcode)
                    {
                        instructionsMatch = false;
                        break;
                    }

                    if (orginalOperand != null &&
                        searchOperand != null &&
                        orginalOperand != searchOperand)
                    {
                        if (orginalOperand.GetType() != typeof(LocalBuilder))
                        {
                            instructionsMatch = false;
                            break;
                        }

                        if (((LocalBuilder) orginalOperand).LocalIndex != (int) searchOperand)
                        {
                            instructionsMatch = false;
                            break;
                        }
                    }
                }
            }
            return instructionsMatch;
        }

        public static void AddBreakDestination(List<CodeInstruction> instructionsList, int currentInstructionIndex,
            Label breakDestination)
        {
            //Since we are going to break inside of some kind of loop, we need to find out where to jump/break to
            //The destination should be one line after the closing bracket of the loop when the exception/break occurs			
            var labels = new HashSet<Label>();

            //gather all labels that exist at or above currentInstructionIndex. the start of our loop is going to be one of these...
            for (var i = 0; i <= currentInstructionIndex; i++)
                foreach (var label in instructionsList[i].labels)
                    labels.Add(label);

            //find first branch that jumps to label above currentInstructionIndex. the first branch opcode found is likely the closing bracket
            for (var i = currentInstructionIndex + 1; i < instructionsList.Count; i++)
                if (instructionsList[i].operand is Label label)
                    if (labels.Contains(label))
                    {
                        instructionsList[i + 1].labels.Add(breakDestination);
                        break;
                    }
        }

        public static void StartTryAndAddBreakDestinationLabel(List<CodeInstruction> instructionsList,
            ref int currentInstructionIndex, Label breakDestination)
        {
            AddBreakDestination(instructionsList, currentInstructionIndex, breakDestination);
            instructionsList[currentInstructionIndex].blocks
                .Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        }

        public static List<CodeInstruction> EndTryStartCatchArgumentExceptionOutOfRange(
            List<CodeInstruction> instructionsList, ref int currentInstructionIndex, ILGenerator iLGenerator,
            Label breakDestination)
        {
            var codeInstructions = new List<CodeInstruction>();
            var handlerEnd = iLGenerator.DefineLabel();
            codeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, handlerEnd));
            var pop = new CodeInstruction(OpCodes.Pop);
            pop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(ArgumentOutOfRangeException)));
            codeInstructions.Add(pop);
            var leaveLoopEnd = new CodeInstruction(OpCodes.Leave, breakDestination);
            leaveLoopEnd.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            codeInstructions.Add(leaveLoopEnd);
            instructionsList[currentInstructionIndex].labels.Add(handlerEnd);
            return codeInstructions;
        }

        public static List<CodeInstruction> UpdateTryCatchCodeInstructions(ILGenerator iLGenerator,
            List<CodeInstruction> instructionsList, int currentInstructionIndex, int searchInstructionsCount)
        {
            var breakDestination = iLGenerator.DefineLabel();
            AddBreakDestination(instructionsList, currentInstructionIndex, breakDestination);
            var finalCodeInstructions = new List<CodeInstruction>();
            instructionsList[currentInstructionIndex].blocks
                .Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            for (var i = 0; i < searchInstructionsCount; i++)
            {
                finalCodeInstructions.Add(instructionsList[currentInstructionIndex]);
                currentInstructionIndex++;
            }

            var handlerEnd = iLGenerator.DefineLabel();
            finalCodeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, handlerEnd));
            var pop = new CodeInstruction(OpCodes.Pop);
            pop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(ArgumentOutOfRangeException)));
            finalCodeInstructions.Add(pop);
            var leaveLoopEnd = new CodeInstruction(OpCodes.Leave, breakDestination);
            leaveLoopEnd.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            finalCodeInstructions.Add(leaveLoopEnd);
            instructionsList[currentInstructionIndex].labels.Add(handlerEnd);
            return finalCodeInstructions;
        }

        public static IEnumerable<CodeInstruction> WrapMethodInInstanceLock(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var i = 0;
            var loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0)
            };
            var lockObject = iLGenerator.DeclareLocal(typeof(object));
            var lockTaken = iLGenerator.DeclareLocal(typeof(bool));
            foreach (var ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
                yield return ci;
            while (i < instructionsList.Count - 1) yield return instructionsList[i++];
            foreach (var ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
                yield return ci;
            yield return instructionsList[i++];
        }

        public static IEnumerable<CodeInstruction> ReplaceFieldsTranspiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            notifiedObjects.Clear();
            foreach (var codeInstruction in instructions)
            {
                if (Prefs.LogVerbose) Log.messageCount = 0; //prevents logging to stop from spam
                var operand = codeInstruction.operand;
                if (operand != null && replaceFields.TryGetValue(operand, out var newObjectInfo))
                    switch (operand)
                    {
                        case FieldInfo fieldInfo:
                        {
                            switch (newObjectInfo)
                            {
                                case FieldInfo newFieldInfo:
                                    if (Prefs.LogVerbose)
                                        if (!notifiedObjects.Contains(newFieldInfo))
                                        {
                                            notifiedObjects.Add(newFieldInfo);
                                            Log.Message("RimThreaded is replacing field: " +
                                                        fieldInfo.DeclaringType + "." + fieldInfo.Name +
                                                        " with field: " + newFieldInfo.DeclaringType + "." +
                                                        newFieldInfo.Name);
                                        }

                                    codeInstruction.operand = newFieldInfo;
                                    break;
                                case Dictionary<OpCode, MethodInfo> newMethodInfoDict:
                                    var newMethodInfo = newMethodInfoDict[codeInstruction.opcode];
                                    if (Prefs.LogVerbose)
                                        if (!notifiedObjects.Contains(newMethodInfo))
                                        {
                                            notifiedObjects.Add(newMethodInfo);
                                            Log.Message("RimThreaded is replacing field: " +
                                                        fieldInfo.DeclaringType + "." + fieldInfo.Name +
                                                        " with CALL method: " + newMethodInfo.DeclaringType + "." +
                                                        newMethodInfo.Name);
                                        }

                                    codeInstruction.opcode = OpCodes.Call;
                                    codeInstruction.operand = newMethodInfo;
                                    break;
                            }

                            break;
                        }
                        case MethodInfo methodInfo:
                        {
                            switch (newObjectInfo)
                            {
                                case FieldInfo newFieldInfo:
                                    if (Prefs.LogVerbose)
                                        if (!notifiedObjects.Contains(newFieldInfo))
                                        {
                                            notifiedObjects.Add(newFieldInfo);
                                            Log.Message("RimThreaded is replacing method: " +
                                                        methodInfo.DeclaringType + "." + methodInfo.Name +
                                                        " with STATIC field: " + newFieldInfo.DeclaringType + "." +
                                                        newFieldInfo.Name);
                                        }

                                    codeInstruction.opcode = OpCodes.Ldsfld;
                                    codeInstruction.operand = newFieldInfo;
                                    break;
                                case MethodInfo newMethodInfo:
                                    if (Prefs.LogVerbose)
                                        if (!notifiedObjects.Contains(newMethodInfo))
                                        {
                                            notifiedObjects.Add(newMethodInfo);
                                            Log.Message("RimThreaded is replacing method: " +
                                                        methodInfo.DeclaringType + "." + methodInfo.Name +
                                                        " with method: " + newMethodInfo.DeclaringType + "." +
                                                        newMethodInfo.Name);
                                        }
                                    codeInstruction.operand = newMethodInfo;
                                    break;
                            }
                            break;
                        }
                    }
                yield return codeInstruction;
            }
        }

        public static IEnumerable<CodeInstruction> Add3Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var i = 0;
            while (i < instructionsList.Count)
            {
                if (i + 3 < instructionsList.Count && instructionsList[i + 3].opcode == OpCodes.Callvirt)
                    if (instructionsList[i + 3].operand is MethodInfo methodInfo)
                        if (methodInfo.Name.Equals("Add") &&
                            methodInfo.DeclaringType.FullName.Contains("System.Collections"))
                        {
                            var lockObject = iLGenerator.DeclareLocal(typeof(object));
                            var lockTaken = iLGenerator.DeclareLocal(typeof(bool));
                            var loadLockObjectInstructions = new List<CodeInstruction>
                            {
                                new CodeInstruction(OpCodes.Ldarg_0)
                            };
                            foreach (var lockInstruction in EnterLock(lockObject, lockTaken, loadLockObjectInstructions,
                                         instructionsList[i])) yield return lockInstruction;
                            yield return instructionsList[i++];
                            yield return instructionsList[i++];
                            yield return instructionsList[i++];
                            yield return instructionsList[i++];
                            foreach (var lockInstruction in ExitLock(iLGenerator, lockObject, lockTaken,
                                         instructionsList[i])) yield return lockInstruction;
                            continue;
                        }

                yield return instructionsList[i++];
            }
        }

        public static void TranspileFieldReplacements(MethodBase original)
        {
            if (Prefs.LogVerbose)
            {
                Log.Message("RimThreaded is TranspilingFieldReplacements for method: " +
                            original.DeclaringType.FullName + "." + original.Name);
                if (original.DeclaringType.FullName.Equals("Verse.Root"))
                {
                    Log.Message("RT Verse.Root");
                    return;
                }
                if (original.Name.Equals("RemoveAll")) Log.Message("RemoveAll");
                Log.Message("RimThreaded is done TranspilingFieldReplacements for method: " +
                            original.DeclaringType.FullName + "." + original.Name);
            }
            harmony.Patch(original, transpiler: replaceFieldsHarmonyTranspiler);
        }

        public static void TranspileLockAdd3(Type original, string methodName, Type[] origType = null)
        {
            harmony.Patch(Method(original, methodName, origType), transpiler: add3Transpiler);
        }

        public static void Prefix(Type original, Type patched, string methodName, Type[] origType = null,
            bool destructive = true, int priority = 0, string finalizer = null, string PatchMethod = null,
            bool NullPatchType = false)
        {
            var oMethod = Method(original, methodName, origType);

            Type[] patch_type = null;
            if (origType != null)
            {
                patch_type = new Type[origType.Length];
                Array.Copy(origType, patch_type, origType.Length);

                if (!oMethod.ReturnType.Name.Equals("Void"))
                {
                    var temp_type = patch_type;
                    patch_type = new Type[temp_type.Length + 1];
                    patch_type[0] = oMethod.ReturnType.MakeByRefType();
                    Array.Copy(temp_type, 0, patch_type, 1, temp_type.Length);
                }

                if (!oMethod.IsStatic)
                {
                    var temp_type = patch_type;
                    patch_type = new Type[temp_type.Length + 1];
                    patch_type[0] = original;
                    Array.Copy(temp_type, 0, patch_type, 1, temp_type.Length);
                }
            }

            if (NullPatchType) patch_type = null;
            var pMethod = Method(patched, methodName, patch_type);

            if (PatchMethod != null) pMethod = Method(patched, PatchMethod, patch_type);

            MethodInfo Finalizer;
            HarmonyMethod FinalizerH = null;
            if (finalizer != null)
            {
                Finalizer = Method(patched, finalizer);
                FinalizerH = new HarmonyMethod(Finalizer, priority);
            }

            harmony.Patch(oMethod, new HarmonyMethod(pMethod, priority), finalizer: FinalizerH);
            if (!destructive) nonDestructivePrefixes.Add(pMethod);
        }

        public static void Postfix(Type original, Type patched, string originalMethodName,
            string patchedMethodName = null)
        {
            var oMethod = Method(original, originalMethodName);
            if (patchedMethodName == null)
                patchedMethodName = originalMethodName;
            var pMethod = Method(patched, patchedMethodName);
            harmony.Patch(oMethod, postfix: new HarmonyMethod(pMethod));
        }

        public static void Transpile(Type original, Type patched, string methodName, Type[] origType = null,
            string[] harmonyAfter = null, int priority = 0, string patchMethod = null)
        {
            var oMethod = Method(original, methodName, origType);
            var pMethod = Method(patched, methodName);
            if (patchMethod != null)
                pMethod = Method(patched, patchMethod);
            var transpilerMethod = new HarmonyMethod(pMethod, priority)
            {
                after = harmonyAfter
            };
            try
            {
                harmony.Patch(oMethod, transpiler: transpilerMethod);
            }
            catch (Exception e)
            {
                Log.Error("Exception Transpiling: " + oMethod + " " + transpilerMethod + " " + e);
            }
        }

        public static void Transpile(Type original, Type patched, string methodName, string newMethodName,
            Type[] origType = null, string[] harmonyAfter = null, int priority = 0, string patchMethod = null)
        {
            var oMethod = Method(original, methodName, origType);
            var pMethod = Method(patched, newMethodName);
            if (patchMethod != null)
                pMethod = Method(patched, patchMethod);
            var transpilerMethod = new HarmonyMethod(pMethod, priority)
            {
                after = harmonyAfter
            };
            try
            {
                harmony.Patch(oMethod, transpiler: transpilerMethod);
            }
            catch (Exception e)
            {
                Log.Error("Exception Transpiling: " + oMethod + " " + transpilerMethod + " " + e);
            }
        }

        public static void TranspileMethodLock(Type original, string methodName, Type[] origType = null)
        {
            var oMethod = Method(original, methodName, origType);
            harmony.Patch(oMethod, transpiler: methodLockTranspiler);
        }


        private static void PatchNonDestructiveFixes()
        {
            //---REQUIRED---
            GameObject_Patch.RunNonDestructivePatches(); //Prevents Game Crash from wooden floors and others
            LongEventHandler_Patch.RunNonDestructivePatches(); //replaced concurrentqueue and run init on new threads


            //---Simple---
            AlertsReadout_Patch
                .RunNonDestructivesPatches(); //this will disable alert checks on ultrafast speed for an added speed boost
            Area_Patch.RunNonDestructivePatches(); //added for sowing area speedup
            CompSpawnSubplant_Transpile.RunNonDestructivePatches(); //fixes bug with royalty spawning subplants
            Designator_Haul_Patch
                .RunNonDestructivePatches(); //add thing to hauling list when user specifically designates it via UI
            CompForbiddable_Patch
                .RunNonDestructivePatches(); //add thing to hauling list when user specifically unforbids it via UI
            PathFinder_Patch.RunNonDestructivePatches(); //simple initialize calcGrid on InitStatusesAndPushStartNode
            TimeControls_Patch.RunNonDestructivePatches(); //allow speed 4
            ZoneManager_Patch.RunNonDestructivePatches(); //recheck growing zone when upon zone grid add
            Zone_Patch.RunNonDestructivePatches(); //recheck growing zone when upon check haul destination call
            // Todo: redo for 1.4
            //HediffGiver_Hypothermia_Transpile.RunNonDestructivePatches(); //speed up for comfy temperature 
            Map_Transpile
                .RunNonDestructivePatches(); //creates separate thread for skyManager.SkyManagerUpdate();            
            BattleLog_Transpile
                .RunNonDestructivePatches(); //if still causing issues, rewrite using ThreadSafeLinkedLists
            //GrammarResolver_Transpile.RunNonDestructivePatches();//reexamine complexity
            //GrammarResolverSimple_Transpile.RunNonDestructivePatches();//reexamine complexity
            HediffSet_Patch.RunNonDestructivePatches(); //TODO - replace 270 instances with ThreadSafeLinkedList
            ThingOwnerThing_Patch.RunNonDestructivePatches(); //reexamine complexity?
            TickList_Patch.RunNonDestructivePatches(); //allows multithreaded calls of thing.tick longtick raretick
            // Todo: reexamine complexity Jobs Of Oppurtunity
            //WorkGiver_ConstructDeliverResources_Transpile.RunNonDestructivePatches(); 
            //WorkGiver_DoBill_Transpile.RunNonDestructivePatches(); //better way to find bills with cache
            //Pawn_RelationsTracker_Patch.RunNonDestructivePatches(); //transpile not needed with new threadsafe simplepools
            PawnCapacitiesHandler_Patch
                .RunNonDestructivePatches(); //TODO fix transpile for 1 of 2 methods try inside of try perhaps?
            Zone_Growing_Patch.RunNonDestructivePatches();

            CellFinder_Patch.RunNonDestructivePatches(); //explosion fix

            GenGrid_Patch.RunNonDestructivePatches(); //explosion fix
            ThinkNode_JoinVoluntarilyJoinableLord_Patch.RunDestructivePatches(); //explosion fix
            Corpse_Patch.RunNonDestructivePatches(); // 1.3 explosion fix
            TransportShipManager_Patch.RunNonDestructivePatches();
            //RestUtility_Patch.RunNonDestructivePatches(); // 1.3 explosion fix - not sure why this causes bug with sleeping
            GrammarResolver_Patch.RunNonDestructivePatches(); //TODO 1.4 reexamine
            Sustainer_Patch.RunNonDestructivePatches();
            //RegionAndRoomQuery_Patch.RunNonDestructivePatches();
            MemoryUtility_Patch.RunNonDestructivePatches(); //Mod memory cleanup on game load, or main menu
            //PawnCapacityUtility_Patch.RunNonDestructivePatches(); //1.4 needed?


            Postfix(typeof(SlotGroup), typeof(HaulingCache),
                nameof(HaulingCache.Notify_AddedCell)); //recheck growing zone when upon stockpile zone grid add
            Postfix(typeof(ListerHaulables), typeof(HaulingCache),
                nameof(HaulingCache.Notify_SlotGroupChanged)); //recheck growing zone when upon other actions
        }

        private static void PatchDestructiveFixes()
        {
            //---REQUIRED---
            TickManager_Patch.RunDestructivePatches(); //Redirects DoSingleTick to RimThreaded

            //---Main-thread-only calls---
            // TODO: These can likely be made more uniform
            AudioSource_Patch.RunDestructivePatches();
            AudioSourceMaker_Patch.RunDestructivePatches();
            ContentFinder_Texture2D_Patch.RunDestructivePatches();
            GenDraw_Patch.RunDestructivePatches(); // DrawMeshNowOrLater (HotSprings and Kijin Race)
            GraphicDatabaseHeadRecords_Patch.RunDestructivePatches();
            Graphics_Patch.RunDestructivePatches(); //Graphics (Giddy-Up and others)
            GUIStyle_Patch.RunDestructivePatches();
            LightningBoltMeshMaker_Patch.RunDestructivePatches();
            MapGenerator_Patch.RunDestructivePatches(); //MapGenerator (Z-levels)
            MaterialPool_Patch.RunDestructivePatches();
            MeshMakerPlanes_Patch.RunDestructivePatches();
            MeshMakerShadows_Patch.RunDestructivePatches();
            MoteBubble_Patch
                .RunDestructivePatches(); //initial 1.3 patch (MaterialPropertyBlock) - main thread UnityEngine.MaterialPropertyBlock
            RenderTexture_Patch.RunDestructivePatches(); //RenderTexture (Giddy-Up)
            SectionLayer_Patch.RunDestructivePatches();
            Texture2D_Patch.RunDestructivePatches(); //Graphics (Giddy-Up)
            Text_Patch.RunDestructivePatches(); //unity get_CurFontStyle on main thread

            //---Multithreaded Ticking---
            FactionManager_Patch.RunDestructivePatches(); //allows multithreaded ticking of factions
            TradeShip_Patch.RunDestructivePatches(); //allows multithreaded ticking of tradeships
            WildPlantSpawner_Patch.RunDestructivePatches(); //allows multithreaded icking of WildPlantSpawner
            WindManager_Patch.RunDestructivePatches(); //allows multithreaded icking of WindManager													  
            WorldComponentUtility_Patch.RunDestructivePatches(); //allows multithreaded icking of WorldComponentUtility
            WorldObjectsHolder_Patch
                .RunDestructivePatches(); //Class was largely overhauled to allow multithreaded ticking for WorldPawns.Tick()
            WorldPawns_Patch
                .RunDestructivePatches(); //Class was largely overhauled to allow multithreaded ticking for WorldPawns.Tick()
            Archive_Patch.RunDestructivePatches(); //explosions fix
            Alert_ColonistLeftUnburied_Patch.RunDestructivePatches(); //1.3 explosions fix
            Alert_MinorBreakRisk_Patch.RunDestructivePatches(); //performance rewrite
            AttackTargetsCache_Patch.RunDestructivesPatches(); //TODO: write ExposeData and change concurrentdictionary
            Battle_Patch.RunDestructivePatches(); //added lock for battle - could use linkedlist
            BeautyUtility_Patch.RunDestructivePatches(); // 1.3 explosion fix null ref

            Building_Door_Patch.RunDestructivePatches(); //strange bug
            Building_PlantGrower_Patch.RunNonDestructivePatches();
            CompCauseGameCondition_Patch.RunDestructivePatches(); //TODO - ThreadSafeLinkedList
            CompSpawnSubplant_Transpile.RunDestructivePatches(); //could use interlock instead
            Corpse_Patch.RunDestructivePatches(); // 1.3 explosion fix
            DateNotifier_Patch.RunDestructivePatches(); //performance boost when playing on only 1 map
            DesignationManager_Patch.RunDestructivePatches(); //added for development build
            District_Patch.RunDestructivePatches(); // 1.3 fix for cachedOpenRoofState null ref - TODO - optimize locks
            DrugAIUtility_Patch.RunDestructivePatches(); //vanilla bug
            DynamicDrawManager_Patch.RunDestructivePatches(); //TODO - candidate for ThreadSafeLinkedList?
            FilthMaker_Patch.RunDestructivePatches(); //replacing a few LINQ queries could possibly improve perf 
            FireUtility_Patch.RunDestructivePatches();
            FleckStatic_Patch.RunDestructivePatches(); // 1.3 explosion fix
            FleckSystemBase_Patch.RunDestructivePatches(); // 1.3 explosion fix
            //FleckSystemBaseFleckThrown_Patch.RunDestructivePatches(); // 1.3 explosion fix
            FoodUtility_Patch.RunDestructivePatches(); //1.3 GetMeatSourceCategory Human from NutrientPasteDispenser

            //GenClosest_Patch.RunDestructivePatches(); replaces RegionwiseBFSWorker - no diff noticable
            //GenCollection_Patch.RunDestructivePatches(); may be fixed now that simplepools work

            //GenPlace_Patch.RunDestructivePatches(); // 1.3 TryPlaceThing null thing after kill

            GenSpawn_Patch.RunDestructivePatches(); //fixes null.destroy - commonly caused by gysers
            GenTemperature_Patch.RunDestructivePatches();
            GenTypes_Patch.RunDestructivePatches(); //possibly occurs with combat extended
            GlobalControlsUtility_Patch.RunDestructivePatches(); //Adds TPS indicator
            GoodwillSituationManager_Patch.RunDestructivePatches(); //initial 1.3 patch
            GoodwillSituationWorker_MemeCompatibility_Patch.RunDestructivePatches(); //initial 1.3 patch
            GridsUtility_Patch.RunDestructivePatches(); // 1.3 explosion fix

            //GrammarResolver_Patch.RunDestructivePatches();
            HediffGiver_Heat_Patch.RunDestructivePatches(); //perf improvment

            HediffSet_Patch.RunDestructivePatches();
            HistoryEventsManager_Patch.RunDestructivePatches(); // 1.3 explosion fix
            ImmunityHandler_Patch.RunDestructivePatches();
            JobDriver_TendPatient_Patch.RunDestructivePatches(); // 1.3 explosion fix
            JobGiver_ExitMap_Patch.RunDestructivePatches(); //explosions fix
            JobGiver_Work_Patch.RunDestructivePatches();
            JobGiver_WanderNearDutyLocation_Patch.RunDestructivePatches();
            ListerBuildingsRepairable_Patch.RunDestructivePatches(); // 1.3 explosion fix
            ListerThings_Patch.RunDestructivePatches();
            //JobMaker_Patch.RunDestructivePatches(); should be fixed by the simplepool patch
            LongEventHandler_Patch.RunDestructivePatches(); //TODO - could use field replacement for conncurrentqueue
            Lord_Patch.RunDestructivePatches();
            LordManager_Patch.RunDestructivePatches();
            LordToil_Siege_Patch.RunDestructivePatches(); //TODO does locks around clears and adds. ThreadSafeLinkedList
            Map_Patch.RunDestructivePatches(); //TODO - discover root cause
            MemoryThoughtHandler_Patch.RunDestructivePatches();
            Messages_Patch.RunDestructivePatches(); // 1.3 explosion fix
            OverlayDrawer_Patch.RunDestructivePatches(); // 1.3 explosion fix
            Pawn_ApparelTracker_Patch.RunDestructivePatches(); //explosions fix			
            Pawn_HealthTracker_Patch.RunDestructivePatches(); //TODO replace with ThreadSafeLinkedList
            Pawn_MindState_Patch.RunDestructivePatches(); //TODO - destructive hack for speed up - maybe not needed
            Pawn_PathFollower_Patch
                .RunDestructivePatches(); // 1.3 null ref pawn?.jobs?.curDriver?.locomotionUrgencySameAs
            Pawn_PlayerSettings_Patch.RunDestructivePatches();
            Pawn_RelationsTracker_Patch.RunDestructivePatches();
            Pawn_RotationTracker_Patch.RunDestructivePatches();
            PawnCapacitiesHandler_Patch.RunDestructivePatches();
            PawnDestinationReservationManager_Patch.RunDestructivePatches();
            PawnPath_Patch.RunDestructivePatches();
            PawnPathPool_Patch
                .RunDestructivePatches(); //removed leak check based on map size, since pool is now a threadstatic
            PawnTextureAtlas_Patch.RunDestructivePatches();
            PawnUtility_Patch.RunDestructivePatches();
            Plant_Patch.RunNonDestructivePatches();
            PlayLog_Patch.RunDestructivePatches();
            PhysicalInteractionReservationManager_Patch
                .RunDestructivePatches(); //TODO: write ExposeData and change concurrent dictionary
            Rand_Patch.RunDestructivePatches(); //Simple
            Reachability_Patch.RunDestructivePatches();
            ReachabilityCache_Patch.RunDestructivePatches(); //TODO simplfy instance.fields
            RealtimeMoteList_Patch.RunDestructivePatches();
            RecipeWorkerCounter_Patch.RunDestructivePatches(); // rexamine purpose
            RecordWorker_TimeGettingJoy_Patch.RunDestructivePatches();
            RegionAndRoomUpdater_Patch.RunDestructivePatches();
            RegionDirtyer_Patch.RunDestructivePatches();

            RegionGrid_Patch.RunDestructivePatches();
            RegionLink_Patch.RunDestructivePatches();
            RegionMaker_Patch.RunDestructivePatches();
            ResourceCounter_Patch.RunDestructivePatches();
            //RoofGrid_Patch is causing problems to the roof notification in 1.3 a fix is also inside the Patch in case  this is needed for something else I am commenting this for now -Senior
            RulePackDef_Patch.RunDestructivePatches(); //explosions fix - grammar
            SeasonUtility_Patch.RunDestructivePatches(); //performance boost
            ShootLeanUtility_Patch
                .RunDestructivePatches(); //TODO: excessive locks, therefore RimThreadedHarmony.Prefix, conncurrent_queue could be transpiled in
            SteadyEnvironmentEffects_Patch.RunDestructivePatches();
            StoreUtility_Patch.RunDestructivePatches(); // 1.3 explosion
            StoryState_Patch.RunDestructivePatches(); //WrapMethodInInstanceLock
            TaleManager_Patch.RunDestructivePatches();
            ThingGrid_Patch.RunDestructivePatches();
            Thing_Patch.RunDestructivePatches(); //Thing_Patch.TakeDamage is a good candidate for transpile
            ThinkNode_SubtreesByTag_Patch.RunDestructivePatches();
            ThinkNode_ForbidOutsideFlagRadius_Patch
                .RunDestructivePatches(); //base method override is double destructive 

            TileTemperaturesComp_Patch.RunDestructivePatches(); //TODO - good simple transpile candidate
            UniqueIDsManager_Patch.RunDestructivePatches(); // Simple use of Interlocked.Increment
            Verb_Patch.RunDestructivePatches(); // TODO: why is this causing null?
            WealthWatcher_Patch.RunDestructivePatches();
            //WorkGiver_GrowerSow_Patch.RunDestructivePatches();


            //complex methods that need further review for simplification
            AttackTargetReservationManager_Patch.RunDestructivePatches();
            BiomeDef_Patch.RunDestructivePatches();
            FloodFiller_Patch
                .RunDestructivePatches(); //FloodFiller - inefficient global lock - threadstatics might help do these concurrently?
            JobQueue_Patch.RunDestructivePatches();
            MapPawns_Patch.RunDestructivePatches(); //TODO: Affects Animal Master Assignment
            MeditationFocusTypeAvailabilityCache_Patch.RunDestructivePatches();
            Pawn_JobTracker_Patch.RunDestructivePatches();
            Pawn_Patch
                .RunDestructivePatches(); // 1) causes strange crash to desktop without error log. 2) not efficient base replacement for PostApplyDamage 
            Region_Patch.RunDestructivePatches();
            ReservationManager_Patch.RunDestructivePatches();
            Room_Patch.RunDestructivePatches();
            SituationalThoughtHandler_Patch
                .RunDestructivePatches(); //TODO replace cachedThoughts with ThreadSafeLinkedList
            ThingOwnerUtility_Patch.RunDestructivePatches(); //TODO fix method reference by index


            //-----SOUND-----
            SampleSustainer_Patch
                .RunDestructivePatches(); // TryMakeAndPlay works better than set_cutoffFrequency, which seems buggy for echo pass filters
            SoundSizeAggregator_Patch.RunDestructivePatches();
            SoundStarter_Patch.RunDestructivePatches(); //disabling this patch stops sounds
            SustainerManager_Patch.RunDestructivePatches();
            //-----END SOUND-----

            Building_Bed_Patch.RunDestructivePatches(); //this patch hides a race condition coming from Room.Map
            RitualObligationTargetWorker_AnyEmptyGrave_Patch.RunDestructivePatches();
            RitualObligationTargetWorker_GraveWithTarget_Patch.RunDestructivePatches();
            PortraitsCache_Patch.RunDestructivePatches();
            RegionLinkDatabase_Patch.RunDestructivePatches();
            //RestUtility_Patch.RunDestructivePatches(); // find bed optimization
            StatWorker_Patch.RunDestructivePatches();
            BodyDef_Patch.RunDestructivePatches();
            SubSoundDef_Patch.RunDestructivePatches();
        }

        private static void PatchModCompatibility()
        {
            AndroidTiers_Patch.Patch();
            AwesomeInventory_Patch.Patch();
            Children_Patch.Patch();
            CombatExteneded_Patch.Patch();
            Dubs_Skylight_Patch.Patch();
            GiddyUpCore_Patch.Patch();
            Hospitality_Patch.Patch();
            JobsOfOppurtunity_Patch.Patch();
            MapReroll_Patch.Patch();
            PawnRules_Patch.Patch();
            ZombieLand_Patch.Patch();
            VEE_Patch.Patch();
            SOS2_Patch.Patch();
            SpeakUp_Patch.Patch();
            RimWar_Patch.Patch();
            TD_Enhancement_Patch.Patch();
            Fluffy_Breakdowns_Patch.Patch();
            Better_Message_Placement_Patch.Patch();
            TurnItOnAndOff_Patch.Patch();
            AlienRace_Patch.Patch();
            DubsBadHygiene_Patch.Patch();
        }

        private static void FullPool_Patch_RunNonDestructivePatches()
        {
            var original = typeof(FullPool<PawnStatusEffecters.LiveEffecter>);
            var patched = typeof(FullPool_Patch<PawnStatusEffecters.LiveEffecter>);
            replaceFields.Add(Method(original, "Get"),
                Method(patched, "Get"));
            replaceFields.Add(Method(original, "Return"),
                Method(patched, "Return"));
        }

        private static void SimplePool_Patch_RunNonDestructivePatches()
        {
            replaceFields.Add(Method(typeof(SimplePool<List<float>>), "Get"),
                Method(typeof(SimplePool_Patch<List<float>>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<List<float>>), "Return"),
                Method(typeof(SimplePool_Patch<List<float>>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<List<float>>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<List<float>>), "get_FreeItemsCount"));

            replaceFields.Add(Method(typeof(SimplePool<List<Pawn>>), "Get"),
                Method(typeof(SimplePool_Patch<List<Pawn>>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<List<Pawn>>), "Return"),
                Method(typeof(SimplePool_Patch<List<Pawn>>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<List<Pawn>>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<List<Pawn>>), "get_FreeItemsCount"));

            replaceFields.Add(Method(typeof(SimplePool<List<Sustainer>>), "Get"),
                Method(typeof(SimplePool_Patch<List<Sustainer>>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<List<Sustainer>>), "Return"),
                Method(typeof(SimplePool_Patch<List<Sustainer>>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<List<Sustainer>>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<List<Sustainer>>), "get_FreeItemsCount"));

            replaceFields.Add(Method(typeof(SimplePool<List<IntVec3>>), "Get"),
                Method(typeof(SimplePool_Patch<List<IntVec3>>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<List<IntVec3>>), "Return"),
                Method(typeof(SimplePool_Patch<List<IntVec3>>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<List<IntVec3>>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<List<IntVec3>>), "get_FreeItemsCount"));

            replaceFields.Add(Method(typeof(SimplePool<List<Thing>>), "Get"),
                Method(typeof(SimplePool_Patch<List<Thing>>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<List<Thing>>), "Return"),
                Method(typeof(SimplePool_Patch<List<Thing>>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<List<Thing>>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<List<Thing>>), "get_FreeItemsCount"));

            replaceFields.Add(Method(typeof(SimplePool<List<Gizmo>>), "Get"),
                Method(typeof(SimplePool_Patch<List<Gizmo>>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<List<Gizmo>>), "Return"),
                Method(typeof(SimplePool_Patch<List<Gizmo>>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<List<Gizmo>>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<List<Gizmo>>), "get_FreeItemsCount"));

            replaceFields.Add(Method(typeof(SimplePool<List<Hediff>>), "Get"),
                Method(typeof(SimplePool_Patch<List<Hediff>>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<List<Hediff>>), "Return"),
                Method(typeof(SimplePool_Patch<List<Hediff>>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<List<Hediff>>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<List<Hediff>>), "get_FreeItemsCount"));

            replaceFields.Add(Method(typeof(SimplePool<HashSet<IntVec3>>), "Get"),
                Method(typeof(SimplePool_Patch<HashSet<IntVec3>>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<HashSet<IntVec3>>), "Return"),
                Method(typeof(SimplePool_Patch<HashSet<IntVec3>>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<HashSet<IntVec3>>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<HashSet<IntVec3>>), "get_FreeItemsCount"));

            replaceFields.Add(Method(typeof(SimplePool<HashSet<Pawn>>), "Get"),
                Method(typeof(SimplePool_Patch<HashSet<Pawn>>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<HashSet<Pawn>>), "Return"),
                Method(typeof(SimplePool_Patch<HashSet<Pawn>>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<HashSet<Pawn>>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<HashSet<Pawn>>), "get_FreeItemsCount"));

            replaceFields.Add(Method(typeof(SimplePool<Job>), "Get"),
                Method(typeof(SimplePool_Patch<Job>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<Job>), "Return"),
                Method(typeof(SimplePool_Patch<Job>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<Job>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<Job>), "get_FreeItemsCount"));

            replaceFields.Add(Method(typeof(SimplePool<Toil>), "Get"),
                Method(typeof(SimplePool_Patch<Toil>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<Toil>), "Return"),
                Method(typeof(SimplePool_Patch<Toil>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<Toil>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<Toil>), "get_FreeItemsCount"));

            replaceFields.Add(Method(typeof(SimplePool<RegionProcessorClosestThingReachable>), "Get"),
                Method(typeof(SimplePool_Patch<RegionProcessorClosestThingReachable>), "Get"));
            replaceFields.Add(Method(typeof(SimplePool<RegionProcessorClosestThingReachable>), "Return"),
                Method(typeof(SimplePool_Patch<RegionProcessorClosestThingReachable>), "Return"));
            replaceFields.Add(Method(typeof(SimplePool<RegionProcessorClosestThingReachable>), "get_FreeItemsCount"),
                Method(typeof(SimplePool_Patch<RegionProcessorClosestThingReachable>), "get_FreeItemsCount"));
        }
        
        private static void Dijkstra_Patch_RunNonDestructivePatches()
        {
            replaceFields.Add(Method(typeof(Dijkstra<IntVec3>), "Run", new[]
                {
                    typeof(IEnumerable<IntVec3>),
                    typeof(Func<IntVec3, IEnumerable<IntVec3>>),
                    typeof(Func<IntVec3, IntVec3, float>),
                    typeof(List<KeyValuePair<IntVec3, float>>),
                    typeof(Dictionary<IntVec3, IntVec3>)
                }),
                Method(typeof(Dijkstra_Patch<IntVec3>), "Run", new[]
                    {
                        typeof(IEnumerable<IntVec3>),
                        typeof(Func<IntVec3, IEnumerable<IntVec3>>),
                        typeof(Func<IntVec3, IntVec3, float>),
                        typeof(List<KeyValuePair<IntVec3, float>>),
                        typeof(Dictionary<IntVec3, IntVec3>)
                    }
                ));

            replaceFields.Add(Method(typeof(Dijkstra<IntVec3>), "Run", new[]
                {
                    typeof(IntVec3),
                    typeof(Func<IntVec3, IEnumerable<IntVec3>>),
                    typeof(Func<IntVec3, IntVec3, float>),
                    typeof(Dictionary<IntVec3, float>),
                    typeof(Dictionary<IntVec3, IntVec3>)
                }),
                Method(typeof(Dijkstra_Patch<IntVec3>), "Run", new[]
                    {
                        typeof(IntVec3),
                        typeof(Func<IntVec3, IEnumerable<IntVec3>>),
                        typeof(Func<IntVec3, IntVec3, float>),
                        typeof(Dictionary<IntVec3, float>),
                        typeof(Dictionary<IntVec3, IntVec3>)
                    }
                ));

            replaceFields.Add(Method(typeof(Dijkstra<Region>), "Run", new[]
                {
                    typeof(IEnumerable<Region>),
                    typeof(Func<Region, IEnumerable<Region>>),
                    typeof(Func<Region, Region, float>),
                    typeof(Dictionary<Region, float>),
                    typeof(Dictionary<Region, Region>)
                }),
                Method(typeof(Dijkstra_Patch<Region>), "Run", new[]
                    {
                        typeof(IEnumerable<Region>),
                        typeof(Func<Region, IEnumerable<Region>>),
                        typeof(Func<Region, Region, float>),
                        typeof(Dictionary<Region, float>),
                        typeof(Dictionary<Region, Region>)
                    }
                ));

            replaceFields.Add(Method(typeof(Dijkstra<int>), "Run", new[]
                {
                    typeof(IEnumerable<int>),
                    typeof(Func<int, IEnumerable<int>>),
                    typeof(Func<int, int, float>),
                    typeof(Dictionary<int, float>),
                    typeof(Dictionary<int, int>)
                }),
                Method(typeof(Dijkstra_Patch<int>), "Run", new[]
                    {
                        typeof(IEnumerable<int>),
                        typeof(Func<int, IEnumerable<int>>),
                        typeof(Func<int, int, float>),
                        typeof(Dictionary<int, float>),
                        typeof(Dictionary<int, int>)
                    }
                ));
        }
#pragma warning disable 649
        [Serializable]
        private class Replacements
        {
            public List<ClassReplacement> ClassReplacements;
        }

        [Serializable]
        private class ClassReplacement
        {
            public string ClassName;
            public bool IgnoreMissing;
            public List<ThreadStaticDetail> ThreadStatics;
        }

        [Serializable]
        private class ThreadStaticDetail
        {
            public string FieldName;
            public string PatchedClassName;
            public bool SelfInitialized;
        }
#pragma warning restore 649
    }
}