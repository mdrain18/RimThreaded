using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class RitualObligationTargetWorker_GraveWithTarget_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(RitualObligationTargetWorker_GraveWithTarget);
            var patched = typeof(RitualObligationTargetWorker_GraveWithTarget_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(LabelExtraPart), new[] {typeof(RitualObligation)});
            RimThreadedHarmony.Prefix(original, patched, nameof(GetTargetInfos));
        }

        internal static IEnumerable<string> GetTargetInfos_E(RitualObligationTargetWorker_GraveWithTarget __instance,
            RitualObligation obligation)
        {
            if (obligation is null || obligation.targetA == null || obligation.targetA.Thing is null ||
                obligation.targetA.Thing.ParentHolder is null || ((Corpse) obligation.targetA.Thing).InnerPawn is null)
            {
                yield return "RitualTargetGraveInfoAbstract".Translate(__instance.parent.ideo.Named("IDEO"));
                yield break;
            }

            var num = obligation.targetA.Thing.ParentHolder is Building_Grave;
            var innerPawn = ((Corpse) obligation.targetA.Thing).InnerPawn;
            var taggedString = "RitualTargetGraveInfo".Translate(innerPawn.Named("PAWN"));
            if (!num)
                taggedString += " (" + "RitualTargetGraveInfoMustBeBuried".Translate(innerPawn.Named("PAWN")) + ")";
            yield return taggedString;
        }

        public static bool GetTargetInfos(RitualObligationTargetWorker_GraveWithTarget __instance,
            ref IEnumerable<string> __result, RitualObligation obligation)
        {
            __result = GetTargetInfos_E(__instance, obligation);
            return false;
        }

        public static bool LabelExtraPart(RitualObligationTargetWorker_GraveWithTarget __instance, ref string __result,
            RitualObligation obligation)
        {
            __result = string.Empty;
            if (obligation == null || obligation.targetA == null || (Corpse) obligation.targetA.Thing == null ||
                ((Corpse) obligation.targetA.Thing).InnerPawn == null) return false;
            __result = ((Corpse) obligation.targetA.Thing).InnerPawn.LabelShort;
            return false;
        }
    }
}