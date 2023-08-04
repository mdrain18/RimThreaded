using System;
using System.Reflection;
using HarmonyLib;
using static HarmonyLib.AccessTools;

namespace RimThreaded.Mod_Patches
{
    internal class ZombieLand_Patch
    {
        public static void Patch()
        {
            var type = TypeByName("ZombieLand.ZombieStateHandler");
            if (type != null)
                foreach (var method in type.GetMethods())
                    if (method.IsDeclaredMember())
                        try
                        {
                            var f = PatchProcessor.ReadMethodBody(method);
                            foreach (var e in f)
                            {
                                if (e.Value is FieldInfo fieldInfo &&
                                    RimThreadedHarmony.replaceFields.ContainsKey(fieldInfo))
                                {
                                    RimThreadedHarmony.TranspileFieldReplacements(method);
                                    break;
                                }

                                if (e.Value is MethodInfo methodInfo &&
                                    RimThreadedHarmony.replaceFields.ContainsKey(methodInfo))
                                {
                                    RimThreadedHarmony.TranspileFieldReplacements(method);
                                    break;
                                }
                            }
                        }
                        catch (NotSupportedException)
                        {
                        }
        }
    }
}