﻿using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    public class Pawn_JobTracker_Patch
    {
        private static readonly object determineNextJobLock = new object();

        internal static void RunDestructivePatches()
        {
            var original = typeof(Pawn_JobTracker);
            var patched = typeof(Pawn_JobTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryFindAndStartJob));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_DamageTaken));
            RimThreadedHarmony.Prefix(original, patched, nameof(DetermineNextJob));
            RimThreadedHarmony.Prefix(original, patched,
                nameof(StartJob)); //conflict with giddyupcore calling MakeDriver
            RimThreadedHarmony.Prefix(original, patched, nameof(TryOpportunisticJob));
        }

        public static bool TryOpportunisticJob(Pawn_JobTracker __instance, ref Job __result, Job job)
        {
            var pawn = __instance.pawn; //added
            if ((int) pawn.def.race.intelligence < 2)
            {
                __result = null;
                return false;
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                __result = null;
                return false;
            }

            if (pawn.Drafted)
            {
                __result = null;
                return false;
            }

            if (job.playerForced)
            {
                __result = null;
                return false;
            }

            if ((int) pawn.RaceProps.intelligence < 2)
            {
                __result = null;
                return false;
            }

            if (!job.def.allowOpportunisticPrefix)
            {
                __result = null;
                return false;
            }

            if (pawn.WorkTagIsDisabled(WorkTags.ManualDumb | WorkTags.Hauling | WorkTags.AllWork))
            {
                __result = null;
                return false;
            }

            if (pawn.InMentalState || pawn.IsBurning())
            {
                __result = null;
                return false;
            }

            if (SlaveRebellionUtility.IsRebelling(pawn))
            {
                __result = null;
                return false;
            }

            var cell = job.targetA.Cell;
            if (!cell.IsValid || cell.IsForbidden(pawn))
            {
                __result = null;
                return false;
            }

            var num = pawn.Position.DistanceTo(cell);
            if (num < 3f)
            {
                __result = null;
                return false;
            }

            //List<Thing> list = pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling(); //removed
            //for (int i = 0; i < list.Count; i++) //removed
            foreach (var thing in HaulingCache.GetClosestHaulableItems(pawn, pawn.Map))
            {
                //Thing thing = list[i]; //removed
                if (thing == null) //added
                    continue; //added
                var num2 = pawn.Position.DistanceTo(thing.Position);
                if (num2 > 30f || num2 > num * 0.5f || num2 + thing.Position.DistanceTo(cell) > num * 1.7f ||
                    pawn.Map.reservationManager.FirstRespectedReserver(thing, pawn) != null ||
                    thing.IsForbidden(pawn) ||
                    !HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, thing, false)) continue;

                var currentPriority = StoreUtility.CurrentStoragePriorityOf(thing);
                var foundCell = IntVec3.Invalid;
                if (!StoreUtility.TryFindBestBetterStoreCellFor(thing, pawn, pawn.Map, currentPriority, pawn.Faction,
                        out foundCell)) continue;

                var num3 = foundCell.DistanceTo(cell);
                if (!(num3 > 50f) && !(num3 > num * 0.6f) &&
                    !(num2 + thing.Position.DistanceTo(foundCell) + num3 > num * 1.7f) && !(num2 + num3 > num) &&
                    pawn.Position.WithinRegions(thing.Position, pawn.Map, 25, TraverseParms.For(pawn)) &&
                    foundCell.WithinRegions(cell, pawn.Map, 25, TraverseParms.For(pawn)))
                {
                    if (DebugViewSettings.drawOpportunisticJobs)
                    {
                        Log.Message("Opportunistic job spawned");
                        pawn.Map.debugDrawer.FlashLine(pawn.Position, thing.Position, 600, SimpleColor.Red);
                        pawn.Map.debugDrawer.FlashLine(thing.Position, foundCell, 600, SimpleColor.Green);
                        pawn.Map.debugDrawer.FlashLine(foundCell, cell, 600, SimpleColor.Blue);
                    }

                    __result = HaulAIUtility.HaulToCellStorageJob(pawn, thing, foundCell, false);
                    return false;
                }
            }

            __result = null;
            return false;
        }

        public static bool StartJob(Pawn_JobTracker __instance,
            Job newJob, JobCondition lastJobEndCondition = JobCondition.None, ThinkNode jobGiver = null,
            bool resumeCurJobAfterwards = false, bool cancelBusyStances = true, ThinkTreeDef thinkTree = null,
            JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false,
            bool? keepCarryingThingOverride = null, bool continueSleeping = false, bool addToJobsThisTick = true)
        {
            __instance.startingNewJob = true;
            Job job = null;
            try
            {
                if (addToJobsThisTick && !fromQueue &&
                    (!Find.TickManager.Paused || __instance.lastJobGivenAtFrame == RealTime.frameCount))
                {
                    __instance.jobsGivenThisTick++;
                    if (Prefs.DevMode)
                        __instance.jobsGivenThisTickTextual = __instance.jobsGivenThisTickTextual + "(" + newJob + ") ";
                }

                __instance.lastJobGivenAtFrame = RealTime.frameCount;
                if (__instance.jobsGivenThisTick > 10)
                {
                    var text = __instance.jobsGivenThisTickTextual;
                    __instance.jobsGivenThisTick = 0;
                    __instance.jobsGivenThisTickTextual = "";
                    __instance.startingNewJob = false;
                    __instance.pawn.ClearReservationsForJob(newJob);
                    JobUtility.TryStartErrorRecoverJob(__instance.pawn,
                        __instance.pawn.ToStringSafe() + " started 10 jobs in one tick. newJob=" +
                        newJob.ToStringSafe() + " jobGiver=" + jobGiver.ToStringSafe() + " jobList=" + text);
                    return false;
                }

                if (__instance.debugLog)
                    __instance.DebugLogEvent(string.Concat("StartJob [", newJob, "] lastJobEndCondition=",
                        lastJobEndCondition, ", jobGiver=", jobGiver, ", cancelBusyStances=",
                        cancelBusyStances.ToString()));
                var stances = __instance.pawn.stances; //changed
                if (cancelBusyStances && stances != null && stances.FullBodyBusy) //changed
                    stances.CancelBusyStanceHard(); //changed
                var asleep = continueSleeping && (__instance.curDriver?.asleep ?? false);


                if (__instance.curJob != null)
                {
                    if (lastJobEndCondition == JobCondition.None)
                    {
                        Log.Warning(string.Concat(__instance.pawn, " starting job ", newJob, " from JobGiver ",
                            newJob.jobGiver, " while already having job ", __instance.curJob,
                            " without a specific job end condition."));
                        lastJobEndCondition = JobCondition.InterruptForced;
                    }

                    if (resumeCurJobAfterwards && __instance.curJob.def.suspendable)
                    {
                        __instance.SuspendCurrentJob(lastJobEndCondition, cancelBusyStances, keepCarryingThingOverride);
                    }
                    else
                    {
                        job = __instance.curDriver.GetFinalizerJob(lastJobEndCondition);
                        if (job != null) keepCarryingThingOverride = keepCarryingThingOverride ?? true;
                        __instance.CleanupCurrentJob(lastJobEndCondition, true, cancelBusyStances,
                            canReturnCurJobToPool, keepCarryingThingOverride);
                    }
                }

                if (newJob == null)
                {
                    Log.Warning(string.Concat(__instance.pawn, " tried to start doing a null job."));
                    return false;
                }

                newJob.startTick = Find.TickManager.TicksGame;
                if (__instance.pawn.Drafted || newJob.playerForced)
                {
                    newJob.ignoreForbidden = true;
                    newJob.ignoreDesignations = true;
                }

                __instance.curJob = newJob;
                __instance.curJob.jobGiverThinkTree = thinkTree;
                __instance.curJob.jobGiver = jobGiver;
                var cDriver = __instance.curJob.MakeDriver(__instance.pawn); //changed
                __instance.curDriver = cDriver; //changed
                var flag = fromQueue;
                if (__instance.curDriver.TryMakePreToilReservations(!flag))
                {
                    var job2 = __instance.TryOpportunisticJob(job, newJob);
                    if (job2 != null)
                    {
                        __instance.jobQueue.EnqueueFirst(newJob);
                        __instance.curJob = null;
                        __instance.ClearDriver();
                        StartJob(__instance, job2, JobCondition.None, null, false, true, null, null, false, false,
                            keepCarryingThingOverride);
                        return false;
                    }

                    if (tag.HasValue)
                    {
                        if (tag == JobTag.Fieldwork && __instance.pawn.mindState.lastJobTag != tag)
                            foreach (var item in PawnUtility.SpawnedMasteredPawns(__instance.pawn))
                                item.jobs.Notify_MasterStartedFieldWork();
                        __instance.pawn.mindState.lastJobTag = tag.Value;
                    }

                    if (__instance.pawn.IsCarrying() &&
                        !(keepCarryingThingOverride ?? !__instance.curJob.def.dropThingBeforeJob))
                    {
                        if (DebugViewSettings.logCarriedBetweenJobs)
                            Log.Message(
                                $"Dropping {__instance.pawn.carryTracker.CarriedThing} before starting job {newJob}");
                        __instance.pawn.carryTracker.TryDropCarriedThing(__instance.pawn.Position, ThingPlaceMode.Near,
                            out var _);
                    }

                    cDriver.SetInitialPosture(); //changed
                    cDriver.Notify_Starting(); //changed
                    cDriver.SetupToils(); //changed
                    cDriver.ReadyForNextToil(); //changed
                }
                else if (flag)
                {
                    __instance.EndCurrentJob(JobCondition.QueuedNoLongerValid);
                }
                else
                {
                    Log.Warning(
                        "TryMakePreToilReservations() returned false for a non-queued job right after StartJob(). This should have been checked before. curJob=" +
                        __instance.curJob.ToStringSafe());
                    __instance.EndCurrentJob(JobCondition.Errored);
                }
            }
            finally
            {
                __instance.startingNewJob = false;
            }

            return false;
        }


        public static bool DetermineNextJob(Pawn_JobTracker __instance, ref ThinkResult __result,
            out ThinkTreeDef thinkTree)
        {
            var constantThinkTreeJob = __instance.DetermineNextConstantThinkTreeJob();
            if (constantThinkTreeJob.Job != null)
            {
                thinkTree = __instance.pawn.thinker.ConstantThinkTree;
                __result = constantThinkTreeJob;
                return false;
            }

            var thinkResult = ThinkResult.NoJob;
            try
            {
                thinkResult =
                    __instance.pawn.thinker.MainThinkNodeRoot.TryIssueJobPackage(__instance.pawn, new JobIssueParams());
            }
            catch (Exception ex)
            {
                JobUtility.TryStartErrorRecoverJob(__instance.pawn,
                    __instance.pawn.ToStringSafe() + " threw exception while determining job (main)", ex);
                thinkTree = null;
                __result = ThinkResult.NoJob;
                return false;
            }

            thinkTree = __instance?.pawn?.thinker?.MainThinkTree; //changed
            if (thinkTree == null) //changed
                thinkResult = ThinkResult.NoJob; //changed
            __result = thinkResult;
            return false;
        }

        public static bool Notify_DamageTaken(Pawn_JobTracker __instance, DamageInfo dinfo)
        {
            var curJob = __instance.curJob;
            if (curJob == null)
                return false;
            var curDriver = __instance.curDriver;
            if (curDriver == null)
                return false;
            curDriver.Notify_DamageTaken(dinfo);
            var curJob2 = __instance.curJob;
            if (curJob2 != curJob || !dinfo.Def.ExternalViolenceFor(__instance.pawn) || !dinfo.Def.canInterruptJobs ||
                curJob2.playerForced || Find.TickManager.TicksGame < __instance.lastDamageCheckTick + 180)
                return false;
            var instigator = dinfo.Instigator;
            if (curJob2.def.checkOverrideOnDamage != CheckJobOverrideOnDamageMode.Always &&
                (curJob2.def.checkOverrideOnDamage != CheckJobOverrideOnDamageMode.OnlyIfInstigatorNotJobTarget ||
                 __instance.curJob.AnyTargetIs((LocalTargetInfo) instigator)))
                return false;
            __instance.lastDamageCheckTick = Find.TickManager.TicksGame;
            __instance.CheckForJobOverride();
            return false;
        }

        public static bool TryFindAndStartJob(Pawn_JobTracker __instance)
        {
            if (__instance.pawn.thinker == null)
            {
                Log.ErrorOnce(string.Concat(__instance.pawn, " did TryFindAndStartJob but had no thinker."), 8573261);
                return false;
            }

            if (__instance.curJob != null)
                Log.Warning(string.Concat(__instance.pawn, " doing TryFindAndStartJob while still having job ",
                    __instance.curJob));
            if (__instance.debugLog) __instance.DebugLogEvent("TryFindAndStartJob");
            /* 1.4
            if (!__instance.CanDoAnyJob())
            {
                if (__instance.debugLog)
                {
                    __instance.DebugLogEvent("   CanDoAnyJob is false. Clearing queue and returning");
                }

                __instance.ClearQueuedJobs();
                return false;
            }
            */
            ThinkTreeDef thinkTree;
            lock (determineNextJobLock) //TODO change to ReservationManager.reservations?
            {
                var result = __instance.DetermineNextJob(out thinkTree);
                if (result.IsValid)
                {
                    __instance.CheckLeaveJoinableLordBecauseJobIssued(result);
                    StartJob(__instance, result.Job, JobCondition.None, result.SourceNode, false, false, thinkTree,
                        result.Tag, result.FromQueue);
                }
            }

            return false;
        }
    }
}