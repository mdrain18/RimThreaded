using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimThreaded.RW_Patches;
using UnityEngine;
using Verse;

namespace RimThreaded.Mod_Patches
{
    public class GeneratePawns_Patch_Transpile
    {
        public static IEnumerable<CodeInstruction> Listener(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var l = instructions.ToList();
            var match = false;


            //Replacement Instructions
            var loadToken = new CodeInstruction(OpCodes.Ldtoken, typeof(Texture2D).GetTypeInfo());
            var resolveToken = new CodeInstruction(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));


            for (var x = 0; x < l.Count; x++)
            {
                var i = l[x];

                if (i.opcode == OpCodes.Call
                    && (MethodInfo) i.operand == TargetMethodHelper())
                {
                    match = true;

                    i.operand = typeof(Resources_Patch).GetMethod("Load");

                    l.Insert(x, resolveToken);
                    l.Insert(x, loadToken);
                }

                yield return l[x];
            }

            if (!match) Log.Error("No IL Instruction found for PawnGroupMakerUtility_Patch.");
        }

        public static MethodBase TargetMethodHelper()
        {
            var i = typeof(Resources).GetMethods().Single(
                m =>
                    m.Name == "Load" &&
                    m.GetGenericArguments().Length == 1 &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == typeof(string)
            );


            return i.MakeGenericMethod(typeof(Texture2D));
        }
    }
}