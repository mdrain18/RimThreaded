using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.Mod_Patches
{
    internal class SpeakUp_Patch
    {
        public static Type GrammarResolver_RandomPossiblyResolvableEntry;

        public static void Patch()
        {
            GrammarResolver_RandomPossiblyResolvableEntry =
                TypeByName("SpeakUp.GrammarResolver_RandomPossiblyResolvableEntry");
            if (GrammarResolver_RandomPossiblyResolvableEntry != null)
            {
                var methodName = nameof(Prefix);
                Log.Message("RimThreaded is patching " + GrammarResolver_RandomPossiblyResolvableEntry.FullName + " " +
                            methodName);
                RimThreadedHarmony.Transpile(GrammarResolver_RandomPossiblyResolvableEntry, typeof(SpeakUp_Patch),
                    methodName);
            }
        }

        public static IEnumerable<CodeInstruction> Prefix(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            for (var i = 0; i < instructionsList.Count; i++)
            {
                var ci = instructionsList[i];
                if (ci.opcode ==
                    OpCodes.Ldarg_S) //&& (ArgumentInfo)ci.operand == Argument(GrammarResolver_RandomPossiblyResolvableEntry, "___rules")
                {
                    ci.opcode = OpCodes.Ldsfld;
                    ci.operand = Field(TypeByName("GrammarResolver_Replacement"), "rules");
                }

                yield return ci;
            }
        }
    }
}