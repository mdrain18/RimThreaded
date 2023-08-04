using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimThreaded.RW_Patches
{
    internal class HediffGiver_Heat_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(HediffGiver_Heat);
            var patched = typeof(HediffGiver_Heat_Patch);
            RimThreadedHarmony.Prefix(original, patched, "OnIntervalPassed");
        }

        public static bool OnIntervalPassed(HediffGiver_Heat __instance, Pawn pawn, Hediff cause)
        {
            var ambientTemperature = pawn.AmbientTemperature;
            var comfortableTemperatureMax = pawn.GetStatValue(StatDefOf.ComfyTemperatureMax);
            var maxTemp = comfortableTemperatureMax + 10f;
            var firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(__instance.hediff);
            if (ambientTemperature > maxTemp)
            {
                var x = ambientTemperature - maxTemp;
                var sevOffset = Mathf.Max(HediffGiver_Heat.TemperatureOverageAdjustmentCurve.Evaluate(x) * 6.45E-05f,
                    0.000375f);
                HealthUtility.AdjustSeverity(pawn, __instance.hediff, sevOffset);
            }
            else if (firstHediffOfDef != null && ambientTemperature < comfortableTemperatureMax)
            {
                var num = Mathf.Clamp(firstHediffOfDef.Severity * 0.027f, 0.0015f, 0.015f);
                firstHediffOfDef.Severity -= num;
            }

            if (pawn.Dead || !pawn.IsNestedHashIntervalTick(60, 420))
                return false;

            var num4 = comfortableTemperatureMax + 150f;
            if (ambientTemperature <= num4)
                return false;

            var x1 = ambientTemperature - num4;
            var num2 = Mathf.Max(
                GenMath.RoundRandom(HediffGiver_Heat.TemperatureOverageAdjustmentCurve.Evaluate(x1) * 0.06f), 3);
            var dinfo = new DamageInfo(DamageDefOf.Burn, num2);
            dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
            pawn.TakeDamage(dinfo);
            if (pawn.Faction == Faction.OfPlayer)
            {
                Find.TickManager.slower.SignalForceNormalSpeed();
                if (MessagesRepeatAvoider.MessageShowAllowed("PawnBeingBurned", 60f))
                    Messages.Message("MessagePawnBeingBurned".Translate(pawn.LabelShort, pawn), pawn,
                        MessageTypeDefOf.ThreatSmall);
            }

            pawn.GetLord()?.ReceiveMemo(HediffGiver_Heat.MemoPawnBurnedByAir);

            return false;
        }
    }
}