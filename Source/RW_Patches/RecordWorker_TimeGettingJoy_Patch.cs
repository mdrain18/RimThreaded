using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class RecordWorker_TimeGettingJoy_Patch
    {
        public static void RunDestructivePatches()
        {
            var original = typeof(RecordWorker_TimeGettingJoy);
            var patched = typeof(RecordWorker_TimeGettingJoy_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(ShouldMeasureTimeNow));
        }

        public static bool ShouldMeasureTimeNow(RecordWorker_TimeGettingJoy __instance, ref bool __result, Pawn pawn)
        {
            __result = pawn?.CurJob?.def?.joyKind != null;
            return false;
        }
    }
}