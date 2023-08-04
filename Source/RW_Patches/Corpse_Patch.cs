using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{
    internal class Corpse_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            var original = typeof(Corpse);
            var patched = typeof(Corpse_Patch);
            RimThreadedHarmony.Transpile(original, patched, nameof(SpawnSetup));
        }

        internal static void RunDestructivePatches()
        {
            var original = typeof(Corpse);
            var patched = typeof(Corpse_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(DrawAt));
        }

        public static IEnumerable<CodeInstruction> SpawnSetup(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            for (var i = 0; i < instructionsList.Count; i++)
            {
                var ci = instructionsList[i];
                if (ci.opcode == OpCodes.Call && (MethodInfo) ci.operand == Method(typeof(Corpse), "get_InnerPawn"))
                {
                    ci.operand = Method(typeof(Corpse_Patch), nameof(SetRotationSouth));
                    yield return ci;
                    i++; //call valuetype Verse.Rot4 Verse.Rot4::get_South()
                    i++; //callvirt instance void Verse.Thing::set_Rotation(valuetype Verse.Rot4)
                    i++; //ldarg.0
                    i++; //NotifyColonistBar();
                    continue;
                }

                yield return ci;
            }
        }

        public static void SetRotationSouth(Corpse __instance)
        {
            var InnerPawn = __instance.InnerPawn;
            if (InnerPawn == null)
                return;
            InnerPawn.Rotation = Rot4.South;
            __instance.NotifyColonistBar();
        }

        public static bool DrawAt(Corpse __instance, Vector3 drawLoc, bool flip = false)
        {
            var InnerPawn = __instance.InnerPawn;
            if (InnerPawn == null)
                return false;
            InnerPawn.Drawer.renderer.RenderPawnAt(drawLoc);
            return false;
        }
    }
}