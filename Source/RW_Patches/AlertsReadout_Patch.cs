using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class AlertsReadout_Patch
    {
        public static void RunNonDestructivesPatches()
        {
            var original = typeof(AlertsReadout);
            var patched = typeof(AlertsReadout_Patch);
            //RimThreadedHarmony.Prefix(original, patched, nameof(AlertsReadoutUpdate), Type.EmptyTypes, false);
        }

        public static bool AlertsReadoutUpdate(AlertsReadout __instance)
        {
            //this will disable alert checks on ultrafast speed for an added speed boost
            return !(Find.TickManager.curTimeSpeed == TimeSpeed.Ultrafast && RimThreadedMod.Settings.disablesomealerts);
        }
    }
}