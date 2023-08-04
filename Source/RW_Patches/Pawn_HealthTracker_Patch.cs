using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{
    public class Pawn_HealthTracker_Patch
    {
        [ThreadStatic] private static bool Pawn_HealthTrackerLockTaken;
        [ThreadStatic] private static HediffSet Pawn_HealthTrackerHediffSet;
        [ThreadStatic] private static bool Pawn_HealthTrackerLockTaken2;
        [ThreadStatic] private static HediffSet Pawn_HealthTrackerHediffSet2;

        internal static void RunDestructivePatches()
        {
            var original = typeof(Pawn_HealthTracker);
            var patched = typeof(Pawn_HealthTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveHediff));
            RimThreadedHarmony.Prefix(original, patched, nameof(RestorePartRecursiveInt));
            RimThreadedHarmony.Prefix(original, patched, nameof(CheckPredicateAfterAddingHediff));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_Resurrected));
            RimThreadedHarmony.Prefix(original, patched, nameof(HealthTick));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetDead)); //optional warning instead of error
            //RimThreadedHarmony.Transpile(original, patched, nameof(CheckForStateChange));
            //RimThreadedHarmony.Prefix(original, patched, nameof(CheckForStateChange));
            var checkForStateChange = Method(original, "CheckForStateChange");
            var checkForStateChangeMonitorEnterMethod = Method(patched, nameof(CheckForStateChangeMonitorEnter));
            RimThreadedHarmony.harmony.Patch(checkForStateChange,
                new HarmonyMethod(checkForStateChangeMonitorEnterMethod, 1000));
            RimThreadedHarmony.nonDestructivePrefixes.Add(checkForStateChangeMonitorEnterMethod);
            RimThreadedHarmony.harmony.Patch(checkForStateChange,
                finalizer: new HarmonyMethod(Method(patched, nameof(CheckForStateChangeMonitorExit)), -1000));
            var postApplyDamage = Method(original, "PostApplyDamage");
            var postApplyDamageEnterMethod = Method(patched, nameof(PostApplyDamageEnter));
            RimThreadedHarmony.nonDestructivePrefixes.Add(postApplyDamageEnterMethod);
            RimThreadedHarmony.harmony.Patch(postApplyDamage, new HarmonyMethod(postApplyDamageEnterMethod, 1000));
            RimThreadedHarmony.harmony.Patch(postApplyDamage,
                finalizer: new HarmonyMethod(Method(patched, nameof(PostApplyDamageExit)), -1000));
        }

        public static bool CheckForStateChangeMonitorEnter(Pawn_HealthTracker __instance, DamageInfo? dinfo,
            Hediff hediff)
        {
            Pawn_HealthTrackerLockTaken = false;
            Pawn_HealthTrackerHediffSet = __instance.hediffSet;
            Monitor.TryEnter(Pawn_HealthTrackerHediffSet, RimThreaded.halfTimeoutMS, ref Pawn_HealthTrackerLockTaken);
            if (Pawn_HealthTrackerLockTaken)
                return true;
            Log.Error("RimThreaded.CheckForStateChange was unable to be obtain lock for Pawn_HealthTracker: " +
                      __instance + " within timeout(MS) : " + RimThreaded.halfTimeoutMS);
            return false;
        }

        public static void CheckForStateChangeMonitorExit(Pawn_HealthTracker __instance, DamageInfo? dinfo,
            Hediff hediff)
        {
            if (Pawn_HealthTrackerLockTaken)
                Monitor.Exit(Pawn_HealthTrackerHediffSet);
        }

        public static bool PostApplyDamageEnter(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            Pawn_HealthTrackerLockTaken2 = false;
            Pawn_HealthTrackerHediffSet2 = __instance.hediffSet;
            Monitor.TryEnter(Pawn_HealthTrackerHediffSet2, RimThreaded.halfTimeoutMS, ref Pawn_HealthTrackerLockTaken2);
            if (Pawn_HealthTrackerLockTaken2)
                return true;
            Log.Error("RimThreaded.CheckForStateChange was unable to be obtain lock for Pawn_HealthTracker: " +
                      __instance + " within timeout(MS) : " + RimThreaded.halfTimeoutMS);
            return false;
        }

        public static void PostApplyDamageExit(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (Pawn_HealthTrackerLockTaken2)
                Monitor.Exit(Pawn_HealthTrackerHediffSet2);
        }

        public static bool SetDead(Pawn_HealthTracker __instance)
        {
            if (__instance.Dead)
                Log.Warning(__instance.pawn + " set dead while already dead."); //changed
            __instance.healthState = PawnHealthState.Dead;
            return false;
        }

        public static bool RemoveHediff(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (__instance.hediffSet == null || __instance.hediffSet.hediffs == null)
                return false;
            hediff.PreRemoved();
            lock (__instance.hediffSet)
            {
                var newHediffs = new List<Hediff>(__instance.hediffSet.hediffs);
                newHediffs.Remove(hediff);
                __instance.hediffSet.hediffs = newHediffs;
            }

            hediff.PostRemoved();
            //__instance.Notify_HediffChanged(null); 1.4 changed?
            __instance.Notify_HediffChanged(hediff);

            return false;
        }

        public static bool RestorePartRecursiveInt(Pawn_HealthTracker __instance, BodyPartRecord part,
            Hediff diffException = null)
        {
            lock (__instance.hediffSet)
            {
                var newHediffs = new List<Hediff>(__instance.hediffSet.hediffs); //added
                //List<Hediff> hediffs = __instance.hediffSet.hediffs; //removed
                for (var index = newHediffs.Count - 1; index >= 0; --index)
                {
                    var hediff = newHediffs[index];
                    if (hediff.Part == part && hediff != diffException && !hediff.def.keepOnBodyPartRestoration)
                    {
                        newHediffs.RemoveAt(index);
                        __instance.hediffSet.hediffs = newHediffs; //added
                        hediff.PostRemoved();
                    }
                }

                for (var index = 0; index < part.parts.Count; ++index)
                    __instance.RestorePartRecursiveInt(part.parts[index], diffException);
            }

            return false;
        }

        public static bool CheckPredicateAfterAddingHediff(Pawn_HealthTracker __instance, ref bool __result,
            Hediff hediff, Func<bool> pred)
        {
            lock (__instance.hediffSet) //added
            {
                var newHediffs = new List<Hediff>(__instance.hediffSet.hediffs); //added
                var missing = __instance.CalculateMissingPartHediffsFromInjury(hediff);
                newHediffs.Add(hediff);
                if (missing != null)
                    newHediffs.AddRange(missing);
                __instance.hediffSet.hediffs = newHediffs; //added
                __instance.hediffSet.DirtyCache();
                var num = pred() ? 1 : 0;
                if (missing != null)
                    newHediffs.RemoveAll(x => missing.Contains(x));
                newHediffs.Remove(hediff);
                __instance.hediffSet.hediffs = newHediffs; //added
                __instance.hediffSet.DirtyCache();
                __result = num != 0;
            }

            return false;
        }

        public static bool Notify_Resurrected(Pawn_HealthTracker __instance)
        {
            lock (__instance.hediffSet) //added
            {
                var newHediffs = new List<Hediff>(__instance.hediffSet.hediffs); //added
                __instance.healthState = PawnHealthState.Mobile;
                newHediffs.RemoveAll(x => x.def.everCurableByItem && x.TryGetComp<HediffComp_Immunizable>() != null);
                newHediffs.RemoveAll(x => x.def.everCurableByItem && x is Hediff_Injury && !x.IsPermanent());
                newHediffs.RemoveAll(x =>
                {
                    if (!x.def.everCurableByItem)
                        return false;
                    if (x.def.lethalSeverity >= 0.0)
                        return true;
                    return x.def.stages != null && x.def.stages.Any(y => y.lifeThreatening);
                });
                newHediffs.RemoveAll(x =>
                    x.def.everCurableByItem && x is Hediff_Injury && x.IsPermanent() &&
                    __instance.hediffSet.GetPartHealth(x.Part) <= 0.0);
                __instance.hediffSet.hediffs = newHediffs; //added
                while (true)
                {
                    var hediffMissingPart = __instance.hediffSet.GetMissingPartsCommonAncestors()
                        .Where(x => !__instance.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(x.Part))
                        .FirstOrDefault();
                    if (hediffMissingPart != null)
                        __instance.RestorePart(hediffMissingPart.Part, checkStateChange: false);
                    else
                        break;
                }

                __instance.hediffSet.DirtyCache();
                if (__instance.ShouldBeDead())
                    __instance.hediffSet.hediffs.RemoveAll(h => !h.def.keepOnBodyPartRestoration);
                __instance.Notify_HediffChanged(null);
            }

            return false;
        }

        public static bool HealthTick(Pawn_HealthTracker __instance)
        {
            var pawn = __instance.pawn; //added
            if (__instance.Dead)
                return false;
            for (var index = __instance.hediffSet.hediffs.Count - 1; index >= 0; --index)
            {
                var hediff = __instance.hediffSet.hediffs[index];
                try
                {
                    hediff.Tick();
                    hediff.PostTick();
                }
                catch (Exception ex1)
                {
                    Log.Error("Exception ticking hediff " + hediff.ToStringSafe() + " for pawn " + pawn.ToStringSafe() +
                              ". Removing hediff... Exception: " + ex1);
                    try
                    {
                        __instance.RemoveHediff(hediff);
                    }
                    catch (Exception ex2)
                    {
                        Log.Error("Error while removing hediff: " + ex2);
                    }
                }

                if (__instance.Dead)
                    return false;
            }

            var flag1 = false;
            lock (__instance.hediffSet) //added
            {
                var newHediffs = new List<Hediff>(__instance.hediffSet.hediffs); //added
                for (var index = newHediffs.Count - 1; index >= 0; --index) //changed
                {
                    var hediff = newHediffs[index];
                    if (hediff.ShouldRemove)
                    {
                        hediff.PreRemoved();
                        newHediffs.RemoveAt(index); //changed
                        __instance.hediffSet.hediffs = newHediffs; //added
                        hediff.PostRemoved();
                        flag1 = true;
                    }
                }
            }

            if (flag1)
                __instance.Notify_HediffChanged(null);
            if (__instance.Dead)
                return false;
            __instance.immunity.ImmunityHandlerTick();
            if (pawn.RaceProps.IsFlesh && pawn.IsHashIntervalTick(600) &&
                (pawn.needs.food == null || !pawn.needs.food.Starving))
            {
                var flag2 = false;
                if (__instance.hediffSet.HasNaturallyHealingInjury())
                {
                    var num = 8f;
                    if (pawn.GetPosture() != PawnPosture.Standing)
                    {
                        num += 4f;
                        var buildingBed = pawn.CurrentBed();
                        if (buildingBed != null)
                            num += buildingBed.def.building.bed_healPerDay;
                    }

                    foreach (var hediff in __instance.hediffSet.hediffs)
                    {
                        var curStage = hediff.CurStage;
                        if (curStage != null && curStage.naturalHealingFactor != -1f)
                            num *= curStage.naturalHealingFactor;
                    }

                    //__instance.hediffSet.GetHediffs<Hediff_Injury>().Where(x => x.CanHealNaturally()).RandomElement().Heal((float)((double)num * (double)pawn.HealthScale * 0.00999999977648258) * pawn.GetStatValue(StatDefOf.InjuryHealingFactor));
                    //1.4
                    __instance.hediffSet.GetHediffs(ref __instance.tmpHediffInjuries, h => h.CanHealNaturally());
                    __instance.tmpHediffInjuries.RandomElement().Heal(num * pawn.HealthScale * 0.01f *
                                                                      pawn.GetStatValue(StatDefOf.InjuryHealingFactor));

                    flag2 = true;
                }

                if (__instance.hediffSet.HasTendedAndHealingInjury() &&
                    (pawn.needs.food == null || !pawn.needs.food.Starving))
                {
                    //1.4
                    //Hediff_Injury hd = __instance.hediffSet.GetHediffs<Hediff_Injury>().Where(x => x.CanHealFromTending()).RandomElement();
                    //hd.Heal((float)(8.0 * (double)GenMath.LerpDouble(0.0f, 1f, 0.5f, 1.5f, Mathf.Clamp01(hd.TryGetComp<HediffComp_TendDuration>().tendQuality)) * (double)pawn.HealthScale * 0.00999999977648258) * pawn.GetStatValue(StatDefOf.InjuryHealingFactor));
                    //flag2 = true;
                    var food = pawn.needs.food;
                    if (food == null || !food.Starving)
                    {
                        __instance.hediffSet.GetHediffs(ref __instance.tmpHediffInjuries, h => h.CanHealFromTending());
                        var hediff_Injury = __instance.tmpHediffInjuries.RandomElement();
                        var tendQuality = hediff_Injury.TryGetComp<HediffComp_TendDuration>().tendQuality;
                        var num4 = GenMath.LerpDouble(0f, 1f, 0.5f, 1.5f, Mathf.Clamp01(tendQuality));
                        hediff_Injury.Heal(8f * num4 * pawn.HealthScale * 0.01f *
                                           pawn.GetStatValue(StatDefOf.InjuryHealingFactor));
                        flag2 = true;
                    }
                }

                if (flag2 && !__instance.HasHediffsNeedingTendByPlayer() &&
                    !HealthAIUtility.ShouldSeekMedicalRest(pawn) && !__instance.hediffSet.HasTendedAndHealingInjury() &&
                    PawnUtility.ShouldSendNotificationAbout(pawn))
                    Messages.Message(
                        "MessageFullyHealed".Translate((NamedArgument) pawn.LabelCap, (NamedArgument) pawn), pawn,
                        MessageTypeDefOf.PositiveEvent);
            }

            if (pawn.RaceProps.IsFlesh && __instance.hediffSet.BleedRateTotal >= 0.1f)
            {
                var num5 = __instance.hediffSet.BleedRateTotal * pawn.BodySize;
                num5 = pawn.GetPosture() != 0 ? num5 * 0.0004f : num5 * 0.004f;
                if (Rand.Value < num5) __instance.DropBloodFilth();
            }

            if (!pawn.IsHashIntervalTick(60))
                return false;
            var hediffGiverSets = pawn.RaceProps.hediffGiverSets;
            if (hediffGiverSets != null)
                for (var index1 = 0; index1 < hediffGiverSets.Count; ++index1)
                {
                    var hediffGivers = hediffGiverSets[index1].hediffGivers;
                    for (var index2 = 0; index2 < hediffGivers.Count; ++index2)
                    {
                        hediffGivers[index2].OnIntervalPassed(pawn, null);
                        if (pawn.Dead)
                            return false;
                    }
                }

            if (pawn.story == null)
                return false;
            var allTraits = pawn.story.traits.allTraits;
            for (var k = 0; k < allTraits.Count; k++)
            {
                if (allTraits[k].Suppressed) continue;
                var currentData = allTraits[k].CurrentData;
                if (!(currentData.randomDiseaseMtbDays > 0f) ||
                    !Rand.MTBEventOccurs(currentData.randomDiseaseMtbDays, 60000f, 60f)) continue;
                BiomeDef biome;
                if (pawn.Tile != -1)
                    biome = Find.WorldGrid[pawn.Tile].biome;
                else
                    biome = DefDatabase<BiomeDef>.GetRandom();
                var incidentDef = DefDatabase<IncidentDef>.AllDefs
                    .Where(d => d.category == IncidentCategoryDefOf.DiseaseHuman)
                    .RandomElementByWeightWithFallback(d => biome.CommonalityOfDisease(d));
                if (incidentDef == null) continue;
                string blockedInfo;
                var list = ((IncidentWorker_Disease) incidentDef.Worker).ApplyToPawns(Gen.YieldSingle(pawn),
                    out blockedInfo);
                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    if (list.Contains(pawn))
                        Find.LetterStack.ReceiveLetter(
                            "LetterLabelTraitDisease".Translate(incidentDef.diseaseIncident.label),
                            "LetterTraitDisease"
                                .Translate(pawn.LabelCap, incidentDef.diseaseIncident.label, pawn.Named("PAWN"))
                                .AdjustedFor(pawn), LetterDefOf.NegativeEvent, pawn);
                    else if (!blockedInfo.NullOrEmpty())
                        Messages.Message(blockedInfo, pawn, MessageTypeDefOf.NeutralEvent);
                }
            }

            return false;
        }
    }
}