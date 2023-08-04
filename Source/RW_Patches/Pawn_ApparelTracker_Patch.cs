using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;

namespace RimThreaded.RW_Patches
{
    internal class Pawn_ApparelTracker_Patch
    {
        [ThreadStatic] public static List<Apparel> tmpApparel = new List<Apparel>();

        internal static void RunDestructivePatches()
        {
            var original = typeof(Pawn_ApparelTracker);
            var patched = typeof(Pawn_ApparelTracker_Patch);
            //RimThreadedHarmony.Prefix(original, patched, nameof(Notify_LostBodyPart));
            RimThreadedHarmony.Transpile(original, patched, nameof(Notify_LostBodyPart));
        }

        public static IEnumerable<CodeInstruction> Notify_LostBodyPart(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var codes = instructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                if (code.opcode == OpCodes.Stloc_2)
                {
                    yield return code;
                    for (var j = i + 1; j < codes.Count; j++)
                    {
                        var tempCode = codes[j];
                        if (tempCode.opcode == OpCodes.Brtrue_S)
                        {
                            var jumpLabel = (Label) tempCode.operand;
                            yield return new CodeInstruction(OpCodes.Ldloc_2);
                            yield return new CodeInstruction(OpCodes.Brfalse_S, jumpLabel);
                            break;
                        }
                    }
                }
                else
                {
                    yield return code;
                }
            }
        }

        //public static bool Notify_LostBodyPart(Pawn_ApparelTracker __instance)
        //{
        //    Pawn_ApparelTracker.tmpApparel.Clear();
        //    for (int index = 0; index < __instance.wornApparel.Count; ++index)
        //        Pawn_ApparelTracker.tmpApparel.Add(__instance.wornApparel[index]);
        //    for (int index = 0; index < Pawn_ApparelTracker.tmpApparel.Count; ++index)
        //    {
        //        Apparel ap = Pawn_ApparelTracker.tmpApparel[index];
        //        if (ap != null && !ApparelUtility.HasPartsToWear(__instance.pawn, ap.def))
        //            __instance.Remove(ap);
        //    }
        //    return false;
        //}
    }
}