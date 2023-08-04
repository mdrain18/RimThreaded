using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Security.Permissions;
using HarmonyLib;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    internal class RimThreadedMod : Mod
    {
        public static RimThreadedSettings Settings;
        public static string replacementsFolder;
        public static string replacementsJsonPath;
        private readonly string RWversion = "1.4";

        public RimThreadedMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimThreadedSettings>();

            replacementsFolder = Path.Combine(content.RootDir, RWversion, "Assemblies");
            replacementsJsonPath = Path.Combine(replacementsFolder, "replacements_" + RWversion + ".json");
            //RimThreaded.Start();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (Settings.modsText.Length == 0)
            {
                Settings.modsText = "Potential RimThreaded mod conflicts :\n";
                Settings.modsText += GetPotentialModConflicts();
            }

            Settings.DoWindowContents(inRect);
            if (Settings.maxThreads != RimThreaded.maxThreads)
            {
                RimThreaded.maxThreads = Math.Max(Settings.maxThreads, 1);
                RimThreaded.RestartAllWorkerThreads();
            }

            RimThreaded.timeoutMS = Math.Max(Settings.timeoutMS, 1000);
            RimThreaded.halfTimeoutMS = new TimeSpan(0, 0, 0, 0, RimThreaded.timeoutMS / 2);
            RimThreaded.timeSpeedNormal = Settings.timeSpeedNormal;
            RimThreaded.timeSpeedFast = Settings.timeSpeedFast;
            RimThreaded.timeSpeedSuperfast = Settings.timeSpeedSuperfast;
            RimThreaded.timeSpeedUltrafast = Settings.timeSpeedUltrafast;
        }

        public static string GetPotentialModConflicts()
        {
            var modsText = "";
            var originalMethods = Harmony.GetAllPatchedMethods();
            foreach (var originalMethod in originalMethods)
            {
                var patches = Harmony.GetPatchInfo(originalMethod);
                if (patches is null)
                {
                }
                else
                {
                    var sortedPrefixes = patches.Prefixes.ToArray();

                    PatchProcessor.GetSortedPatchMethods(originalMethod, sortedPrefixes);
                    var isRimThreadedPrefixed = false;
                    var modsText1 = "";
                    foreach (var patch in sortedPrefixes)
                    {
                        if (patch.owner.Equals("majorhoff.rimthreaded") &&
                            !RimThreadedHarmony.nonDestructivePrefixes.Contains(patch.PatchMethod) &&
                            (patches.Prefixes.Count > 1 || patches.Postfixes.Count > 0 ||
                             patches.Transpilers.Count > 0))
                        {
                            isRimThreadedPrefixed = true;
                            modsText1 = "\n  ---Patch method: " + patch.PatchMethod.DeclaringType.FullName + " " +
                                        patch.PatchMethod + "---\n";
                            modsText1 += "  RimThreaded priority: " + patch.priority + "\n";
                            break;
                        }
                    }

                    if (isRimThreadedPrefixed)
                    {
                        var rimThreadedPatchFound = false;
                        var headerPrinted = false;
                        foreach (var patch in sortedPrefixes)
                        {
                            if (patch.owner.Equals("majorhoff.rimthreaded"))
                                rimThreadedPatchFound = true;
                            if (!patch.owner.Equals("majorhoff.rimthreaded") && rimThreadedPatchFound)
                            {
                                //Settings.modsText += "method: " + patch.PatchMethod + " - ";
                                if (!headerPrinted)
                                    modsText += modsText1;
                                headerPrinted = true;
                                modsText += "  owner: " + patch.owner + " - ";
                                modsText += "  priority: " + patch.priority + "\n";
                            }
                        }

                        foreach (var patch in patches.Transpilers)
                        {
                            if (!headerPrinted)
                                modsText += modsText1;
                            headerPrinted = true;
                            //Settings.modsText += "method: " + patch.PatchMethod + " - ";
                            modsText += "  owner: " + patch.owner + " - ";
                            modsText += "  priority: " + patch.priority + "\n";
                        }
                    }
                }
            }

            return modsText;
        }

        public static void ExportTranspiledMethods()
        {
            var aName = new AssemblyName("RimWorldTranspiles");
            var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
            var Constructor2 = typeof(SecurityPermissionAttribute).GetConstructors()[0];
            var skipVerificationProperty = Property(typeof(SecurityPermissionAttribute), "SkipVerification");
            var modBuilder = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
            var ModAtt = new UnverifiableCodeAttribute();
            var Constructor = ModAtt.GetType().GetConstructors()[0];
            var ObjectArray = new object[0];
            var ModAttribBuilder = new CustomAttributeBuilder(Constructor, ObjectArray);
            modBuilder.SetCustomAttribute(ModAttribBuilder);
            var typeBuilders = new Dictionary<string, TypeBuilder>();
            var originalMethods = Harmony.GetAllPatchedMethods();
            foreach (var originalMethod in originalMethods)
            {
                var patches = Harmony.GetPatchInfo(originalMethod);
                var transpiledCount = patches.Transpilers.Count;
                if (transpiledCount == 0)
                    continue;
                if (originalMethod is MethodInfo methodInfo) // add support for constructors as well
                {
                    var returnType = methodInfo.ReturnType;
                    var typeTranspiled = originalMethod.DeclaringType.FullName + "_Transpiled";
                    if (!typeBuilders.TryGetValue(typeTranspiled, out var tb))
                    {
                        tb = modBuilder.DefineType(typeTranspiled, TypeAttributes.Public);
                        typeBuilders[typeTranspiled] = tb;
                    }

                    var parameterInfos = methodInfo.GetParameters();
                    var types = new List<Type>();

                    var parameterOffset = 1;
                    if (!methodInfo.Attributes.HasFlag(MethodAttributes.Static))
                    {
                        types.Add(methodInfo.DeclaringType);
                        parameterOffset = 2;
                    }

                    foreach (var parameterInfo in parameterInfos) types.Add(parameterInfo.ParameterType);
                    var mb = tb.DefineMethod(originalMethod.Name, MethodAttributes.Public | MethodAttributes.Static,
                        returnType, types.ToArray());
                    if (typeTranspiled.Equals("Verse.PawnGenerator_Transpiled") && !originalMethod.Name.Equals(""))
                        Log.Message(originalMethod.Name);
                    if (!methodInfo.Attributes.HasFlag(MethodAttributes.Static))
                    {
                        var pa = new ParameterAttributes();
                        var pb = mb.DefineParameter(1, pa, methodInfo.DeclaringType.Name);
                    }

                    foreach (var parameterInfo in parameterInfos)
                    {
                        var pa = new ParameterAttributes();
                        if (parameterInfo.IsOut) pa |= ParameterAttributes.Out;
                        if (parameterInfo.IsIn) pa |= ParameterAttributes.In;
                        if (parameterInfo.IsLcid) pa |= ParameterAttributes.Lcid;
                        if (parameterInfo.IsOptional) pa |= ParameterAttributes.Optional;
                        if (parameterInfo.IsRetval) pa |= ParameterAttributes.Retval;
                        if (parameterInfo.HasDefaultValue) pa |= ParameterAttributes.HasDefault;
                        var pb = mb.DefineParameter(parameterInfo.Position + parameterOffset, pa, parameterInfo.Name);
                        if (parameterInfo.HasDefaultValue && parameterInfo.DefaultValue != null)
                            pb.SetConstant(parameterInfo.DefaultValue);
                    }

                    var il = mb.GetILGenerator();
                    
                }
            }

            foreach (var tb in typeBuilders) tb.Value.CreateType();
            ab.Save(aName.Name + ".dll");


            //ReImport DLL and create detour
            var loadedAssembly = Assembly.UnsafeLoadFrom(aName.Name + ".dll");
            var transpiledTypes = loadedAssembly.DefinedTypes;
            var tTypeDictionary = new Dictionary<string, Type>();
            foreach (var transpiledType in transpiledTypes)
                tTypeDictionary.Add(transpiledType.FullName, transpiledType.AsType());

            foreach (var originalMethod in originalMethods)
            {
                var patches = Harmony.GetPatchInfo(originalMethod);
                var transpiledCount = patches.Transpilers.Count;
                if (transpiledCount > 0)
                    if (originalMethod is MethodInfo methodInfo) // add support for constructors as well
                    {
                        var transpiledType = tTypeDictionary[originalMethod.DeclaringType.FullName + "_Transpiled"];
                        var parameterInfos = methodInfo.GetParameters();
                        var types = new List<Type>();

                        if (!methodInfo.Attributes.HasFlag(MethodAttributes.Static))
                            types.Add(methodInfo.DeclaringType);
                        foreach (var parameterInfo in parameterInfos) types.Add(parameterInfo.ParameterType);
                        var replacement = Method(transpiledType, originalMethod.Name, types.ToArray());
                        Memory.DetourMethod(originalMethod, replacement);
                    }
            }
        }
        public override string SettingsCategory()
        {
            return "RimThreaded";
        }
    }
}