using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class HediffGiver_Hypothermia_Patch
    {
        public static bool OnIntervalPassed(HediffGiver_Hypothermia __instance, Pawn pawn, Hediff cause)
        {
            var ambientTemperature = pawn.AmbientTemperature;
            //FloatRange floatRange = pawn.ComfortableTemperatureRange(); //REMOVED
            //FloatRange floatRange2 = pawn.SafeTemperatureRange(); //REMOVED
            var comfortableTemperatureMin = pawn.GetStatValue(StatDefOf.ComfyTemperatureMin); //ADDED
            var minTemp = comfortableTemperatureMin - 10f; //ADDED
            var hediffSet = pawn.health.hediffSet;
            var hediffDef = pawn.RaceProps.FleshType == FleshTypeDefOf.Insectoid
                ? __instance.hediffInsectoid
                : __instance.hediff;
            var firstHediffOfDef = hediffSet.GetFirstHediffOfDef(hediffDef);
            //if (ambientTemperature < floatRange2.min) //REMOVED
            if (ambientTemperature < minTemp) //ADDED
            {
                //float a = Mathf.Abs(ambientTemperature - floatRange2.min) * 6.45E-05f; //REMOVED
                var a = Mathf.Abs(ambientTemperature - minTemp) * 6.45E-05f; //ADDED
                a = Mathf.Max(a, 0.00075f);
                HealthUtility.AdjustSeverity(pawn, hediffDef, a);
                if (pawn.Dead)
                    return false;
            }

            if (firstHediffOfDef == null)
                return false;
            //if (ambientTemperature > floatRange.min) //REMOVED
            if (ambientTemperature > comfortableTemperatureMin) //ADDED
            {
                var value = firstHediffOfDef.Severity * 0.027f;
                value = Mathf.Clamp(value, 0.0015f, 0.015f);
                firstHediffOfDef.Severity -= value;
            }
            else if (pawn.RaceProps.FleshType != FleshTypeDefOf.Insectoid && ambientTemperature < 0f &&
                     firstHediffOfDef.Severity > 0.37f)
            {
                var num = 0.025f * firstHediffOfDef.Severity;
                if (Rand.Value < num && pawn.RaceProps.body.AllPartsVulnerableToFrostbite
                        .Where(x => !hediffSet.PartIsMissing(x))
                        .TryRandomElementByWeight(x => x.def.frostbiteVulnerability, out var result))
                {
                    var num2 = Mathf.CeilToInt(result.def.hitPoints * 0.5f);
                    var dinfo = new DamageInfo(DamageDefOf.Frostbite, num2, 0f, -1f, null, result);
                    pawn.TakeDamage(dinfo);
                }
            }

            return false;
        }
    }
}