using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{
    public class BattleLog_Transpile
    {
        public static object addLogEntryLock = new object();

        internal static void RunNonDestructivePatches()
        {
            var original = typeof(BattleLog);
            var patched = typeof(BattleLog_Transpile);
            RimThreadedHarmony.Transpile(original, patched, nameof(Add));
        }

        public static IEnumerable<CodeInstruction> Add(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var i = 0;
            var lockObjectType = typeof(object);
            var loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, Field(typeof(BattleLog_Transpile), "addLogEntryLock"))
            };
            var lockObject = iLGenerator.DeclareLocal(lockObjectType);
            var lockTaken = iLGenerator.DeclareLocal(typeof(bool));
            foreach (var ci in RimThreadedHarmony.EnterLock(
                         lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
                yield return ci;

            while (i < instructionsList.Count - 1) yield return instructionsList[i++];
            foreach (var ci in RimThreadedHarmony.ExitLock(
                         iLGenerator, lockObject, lockTaken, instructionsList[i]))
                yield return ci;

            while (i < instructionsList.Count) yield return instructionsList[i++];
        }
    }
}