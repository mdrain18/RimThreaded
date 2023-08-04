using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class Zone_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            var original = typeof(Zone);
            var patched = typeof(Zone_Patch);
            RimThreadedHarmony.Postfix(original, patched, "CheckAddHaulDestination");
        }

        public static void CheckAddHaulDestination(Zone __instance)
        {
            if (Current.ProgramState == ProgramState.Playing)
                if (__instance is Zone_Growing zone)
                    //Log.Message("Adding growing zone cell to awaiting plant cells");
                    foreach (var c in zone.cells)
                        JumboCell.ReregisterObject(zone.Map, c, RimThreaded.plantSowing_Cache);
        }
    }
}