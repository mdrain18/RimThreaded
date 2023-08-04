using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace RimThreaded.RW_Patches
{
    public class Mesh_Transpile
    {
        public static IEnumerable<CodeInstruction> Mesh(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var i = 0;
            while (i < instructionsList.Count)
                if (
                    instructionsList[i].opcode == OpCodes.Call &&
                    (MethodInfo) instructionsList[i].operand == AccessTools.Method(typeof(Mesh), "InternalCreate")
                )
                {
                }
                else
                {
                    yield return instructionsList[i];
                    i++;
                }
        }
    }
}