using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class Zone_Growing_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            var original = typeof(Zone_Growing);
            var patched = typeof(Zone_Growing_Patch);
            RimThreadedHarmony.Postfix(original, patched, nameof(SetPlantDefToGrow));
        }

        public static void SetPlantDefToGrow(Zone_Growing __instance, ThingDef plantDef)
        {
            if (Current.ProgramState == ProgramState.Playing)
                foreach (var c in __instance.cells)
                    JumboCell.ReregisterObject(__instance.Map, c, RimThreaded.plantSowing_Cache);
        }
    }
}