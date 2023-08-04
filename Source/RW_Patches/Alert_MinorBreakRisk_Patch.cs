using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class Alert_MinorBreakRisk_Patch
    {
        public static void RunDestructivePatches()
        {
            var original = typeof(Alert_MinorBreakRisk);
            var patched = typeof(Alert_MinorBreakRisk_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(GetReport));
        }

        public static bool GetReport(Alert_MinorBreakRisk __instance, ref AlertReport __result)
        {
            var pawnsAtRiskMinorResult = new List<Pawn>();
            var pawnList = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoCryptosleep;
            for (var i = 0; i < pawnList.Count; i++)
            {
                var item = pawnList[i];
                if (item.Downed || item.MentalStateDef != null) continue;
                var curMood = item.mindState.mentalBreaker.CurMood;
                if (curMood < item.mindState.mentalBreaker.BreakThresholdMajor) return false;
                if (curMood < item.mindState.mentalBreaker.BreakThresholdMinor) pawnsAtRiskMinorResult.Add(item);
            }

            __result = AlertReport.CulpritsAre(pawnsAtRiskMinorResult);
            return false;
        }
    }
}