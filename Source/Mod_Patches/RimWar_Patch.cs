using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld.Planet;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    internal class RimWar_Patch
    {
        public static Type RimWar_Planet_WorldUtility;

        public static void Patch()
        {
            RimWar_Planet_WorldUtility = TypeByName("RimWar.Planet.WorldUtility");
            if (RimWar_Planet_WorldUtility != null)
            {
                var methodName = nameof(GetWorldObjectsInRange);
                Log.Message("RimThreaded is patching " + RimWar_Planet_WorldUtility.FullName + " " + methodName);
                Transpile(RimWar_Planet_WorldUtility, typeof(RimWar_Patch), methodName);
            }
        }

        public static IEnumerable<CodeInstruction> GetWorldObjectsInRange(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            for (var i = 0; i < instructionsList.Count; i++)
            {
                var codeInstruction = instructionsList[i];
                if (codeInstruction.opcode == OpCodes.Callvirt &&
                    (MethodInfo) codeInstruction.operand == Method(typeof(WorldObject), "get_Tile"))
                {
                    var worldObject = iLGenerator.DeclareLocal(typeof(WorldObject));
                    yield return new CodeInstruction(OpCodes.Stloc, worldObject);
                    yield return new CodeInstruction(OpCodes.Ldloc, worldObject);
                    //yield return new CodeInstruction(OpCodes.Ldnull);
                    //yield return new CodeInstruction(OpCodes.Ceq);
                    var label = (Label) instructionsList[i + 10].operand;
                    //yield return new CodeInstruction(OpCodes.Brtrue, label);
                    yield return new CodeInstruction(OpCodes.Brfalse, label);
                    yield return new CodeInstruction(OpCodes.Ldloc, worldObject);
                }

                yield return codeInstruction;
            }
        }
    }
}