using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.RW_Patches
{
    public class TileTemperaturesComp_Transpile
    {
        public static IEnumerable<CodeInstruction> WorldComponentTick(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var i = 0;
            var loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, Field(typeof(TileTemperaturesComp_Patch), "worldComponentTickLock"))
            };
            var lockObject = iLGenerator.DeclareLocal(typeof(object));
            var lockTaken = iLGenerator.DeclareLocal(typeof(bool));
            foreach (var ci in EnterLock(
                         lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
                yield return ci;
            while (i < instructionsList.Count - 1) yield return instructionsList[i++];
            foreach (var ci in ExitLock(
                         iLGenerator, lockObject, lockTaken, instructionsList[i]))
                yield return ci;
            yield return instructionsList[i++];
        }
    }
}