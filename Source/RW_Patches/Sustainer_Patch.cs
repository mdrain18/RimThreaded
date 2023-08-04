using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.Sound;
using static HarmonyLib.AccessTools;


namespace RimThreaded.RW_Patches
{
    internal class Sustainer_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            var original = typeof(Sustainer);
            var patched = typeof(Sustainer_Patch);
            var oMethod = Constructor(original);
            var transpilerMethod = new HarmonyMethod(Method(patched, nameof(TranspileCtor)));
            RimThreadedHarmony.harmony.Patch(oMethod, transpiler: transpilerMethod);
        }

        public static IEnumerable<CodeInstruction> TranspileCtor(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var ciList = instructions.ToList();
            var i = 0;
            while (i < ciList.Count)
            {
                var ci = ciList[i];
                if (ci.opcode == OpCodes.Call && (MethodInfo) ci.operand == Method(typeof(Find), "get_SoundRoot"))
                {
                    var ci1 = ciList[i + 1];
                    if (ci1.opcode == OpCodes.Ldfld && (FieldInfo) ci1.operand ==
                        Field(typeof(SoundRoot), nameof(SoundRoot.sustainerManager)))
                    {
                        var ci2 = ciList[i + 2];
                        if (ci2.opcode == OpCodes.Ldarg_0)
                        {
                            var ci3 = ciList[i + 3];
                            if (ci3.opcode == OpCodes.Callvirt && (MethodInfo) ci3.operand ==
                                Method(typeof(SustainerManager), nameof(SustainerManager.RegisterSustainer)))
                            {
                                yield return new CodeInstruction(OpCodes.Ldarg_0);
                                yield return new CodeInstruction(OpCodes.Call,
                                    Method(typeof(Sustainer_Patch), nameof(SustainerManagerRegisterSustainer)));
                                i += 4;
                                continue;
                            }
                        }
                        else if (ci2.opcode == OpCodes.Callvirt && (MethodInfo) ci2.operand ==
                                 Method(typeof(SustainerManager), nameof(SustainerManager.UpdateAllSustainerScopes)))
                        {
                            yield return new CodeInstruction(OpCodes.Call,
                                Method(typeof(Sustainer_Patch), nameof(SustainerManagerUpdateAllSustainerScopes)));
                            i += 3;
                            continue;
                        }
                    }
                }

                yield return ci;
                i++;
            }
        }

        public static void SustainerManagerRegisterSustainer(Sustainer sustainer)
        {
            var soundRoot = Find.SoundRoot;
            if (soundRoot == null)
            {
                Log.Error("SoundRoot is null");
                return;
            }

            var sustainerManager = soundRoot.sustainerManager;
            if (sustainerManager == null)
            {
                Log.Error("SustainerManager is null");
                return;
            }

            sustainerManager.RegisterSustainer(sustainer);
        }

        public static void SustainerManagerUpdateAllSustainerScopes()
        {
            var soundRoot = Find.SoundRoot;
            if (soundRoot == null)
            {
                Log.Error("SoundRoot is null");
                return;
            }

            var sustainerManager = soundRoot.sustainerManager;
            if (sustainerManager == null)
            {
                Log.Error("SustainerManager is null");
                return;
            }

            sustainerManager.UpdateAllSustainerScopes();
        }
    }
}