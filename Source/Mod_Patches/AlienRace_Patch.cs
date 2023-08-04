using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimThreaded.RW_Patches;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    internal class AlienRace_Patch
    {
        public static void Patch()
        {
            var ARHarmonyPatches = TypeByName("AlienRace.HarmonyPatches");
            if (ARHarmonyPatches != null)
            {
                var methodName = nameof(HediffSet_Patch.AddDirect);
                Log.Message("RimThreaded is patching " + typeof(HediffSet_Patch).FullName + " " + methodName);
                Transpile(typeof(HediffSet_Patch), typeof(AlienRace_Patch), methodName);


                methodName = nameof(HediffSet_Patch.CacheMissingPartsCommonAncestors);
                Log.Message("RimThreaded is patching " + typeof(HediffSet_Patch).FullName + " " + methodName);
                Transpile(typeof(HediffSet_Patch), typeof(AlienRace_Patch), methodName);
            }
        }


        public static IEnumerable<CodeInstruction> AddDirect(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var ARHarmonyPatches = TypeByName("AlienRace.HarmonyPatches");
            return (IEnumerable<CodeInstruction>) ARHarmonyPatches.GetMethod("BodyReferenceTranspiler")
                .Invoke(null, new object[] {instructions});
        }

        public static IEnumerable<CodeInstruction> CacheMissingPartsCommonAncestors(
            IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            var ARHarmonyPatches = TypeByName("AlienRace.HarmonyPatches");
            return (IEnumerable<CodeInstruction>) ARHarmonyPatches.GetMethod("BodyReferenceTranspiler")
                .Invoke(null, new object[] {instructions});
        }
    }
}