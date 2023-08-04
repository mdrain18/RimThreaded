using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class RitualObligationTargetWorker_AnyEmptyGrave_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(RitualObligationTargetWorker_AnyEmptyGrave);
            var patched = typeof(RitualObligationTargetWorker_AnyEmptyGrave_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(LabelExtraPart), new[] {typeof(RitualObligation)});
        }

        public static bool LabelExtraPart(RitualObligationTargetWorker_AnyEmptyGrave __instance, ref string __result,
            RitualObligation obligation)
        {
            __result = string.Empty;
            if (obligation == null || obligation.targetA == null || (Pawn) obligation.targetA.Thing == null)
                return false;
            __result = ((Pawn) obligation.targetA.Thing).LabelShort;
            return false;
        }
    }
}