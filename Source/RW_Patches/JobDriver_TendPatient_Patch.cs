using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class JobDriver_TendPatient_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(JobDriver_TendPatient);
            var patched = typeof(JobDriver_TendPatient_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(get_Deliveree));
        }

        public static bool get_Deliveree(JobDriver_TendPatient __instance, ref Pawn __result)
        {
            __result = (Pawn) __instance?.job?.targetA.Thing;
            return false;
        }
    }
}