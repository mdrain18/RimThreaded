﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    public class JobGiver_Work_Patch
    {
        private static readonly HashSet<WorkGiverDef> workGiversForClosestThingReachable2 = new HashSet<WorkGiverDef>();

        private static readonly HashSet<ThingRequestGroup> thingRequestGroupsCached = new HashSet<ThingRequestGroup>
        {
            //ThingRequestGroup.Seed,
            //ThingRequestGroup.Blueprint,
            //ThingRequestGroup.Refuelable,
            //ThingRequestGroup.Transporter,
            //ThingRequestGroup.BuildingFrame,
            //ThingRequestGroup.PotentialBillGiver,
            //ThingRequestGroup.Filth
            //,ThingRequestGroup.BuildingArtificial
        };

        private static WorkGiverDef haulGeneral;

        internal static void RunDestructivePatches()
        {
            var original = typeof(JobGiver_Work);
            var patched = typeof(JobGiver_Work_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryIssueJobPackage));
            BuildWorkGiverList(); //TODO: Move to initialize?
        }
        //public static HashSet<ThingRequestGroup> workGroups = new HashSet<ThingRequestGroup>();

        public static void BuildWorkGiverList()
        {
            //Use method ClosestThingReachable2 for these workGiverDefs
            //ClosestThingReachable2 is same as ClosestThingReachable, except it checks validator before canreach
            var workGivers = new HashSet<string>
            {
                "DoctorFeedAnimals",
                "DoctorFeedHumanlikes",
                "DoctorTendToAnimals",
                "DoctorTendToHumanlikes",
                "DoBillsUseCraftingSpot",
                "DoctorTendEmergency",
                "HaulCorpses",
                "FillFermentingBarrel",
                "HandlingFeedPatientAnimals",
                "Train",
                "VisitSickPawn",
                "DoBillsButcherFlesh",
                "DoBillsCook",
                "DoBillsMakeApparel",
                "ExecuteGuiltyColonist",
                "TakeRoamingAnimalsToPen",
                "TakeToPen",
                "RebalanceAnimalsInPens"
            };
            foreach (var workGiverDef in DefDatabase<WorkGiverDef>.AllDefs)
            {
                if (workGivers.Contains(workGiverDef.defName))
                    workGiversForClosestThingReachable2.Add(workGiverDef);
                if (workGiverDef.defName.Equals("HaulGeneral"))
                    haulGeneral = workGiverDef;
            }
        }

#if DEBUG
        [ThreadStatic]
        static Stopwatch s1;
        [ThreadStatic]
        static Stopwatch s2;
#endif

        public static bool TryIssueJobPackage(JobGiver_Work __instance, ref ThinkResult __result, Pawn pawn,
            JobIssueParams jobParams)
        {
#if DEBUG
            if (s1 == null)
                s1 = new Stopwatch();
            if (s2 == null)
                s2 = new Stopwatch();
            s1.Restart();
            s2.Restart();
#endif
            if (__instance.emergency && pawn.mindState.priorityWork.IsPrioritized)
            {
                var workGiversByPriority = pawn.mindState.priorityWork.WorkGiver.workType.workGiversByPriority;
                for (var i = 0; i < workGiversByPriority.Count; i++)
                {
                    var worker = workGiversByPriority[i].Worker;
                    if (__instance.WorkGiversRelated(pawn.mindState.priorityWork.WorkGiver, worker.def))
                    {
                        var job = GiverTryGiveJobPrioritized(__instance, pawn, worker,
                            pawn.mindState.priorityWork.Cell);
                        if (job != null)
                        {
                            job.playerForced = true;
                            __result = new ThinkResult(job, __instance, workGiversByPriority[i].tagToGive);
                            // Log.Message(s1.ElapsedMilliseconds.ToString() + " s10");
                            return false;
                        }
                    }
                }

                pawn.mindState.priorityWork.Clear();
            }

            var list = !__instance.emergency
                ? pawn.workSettings.WorkGiversInOrderNormal
                : pawn.workSettings.WorkGiversInOrderEmergency;
            var num = -999;
            var bestTargetOfLastPriority = TargetInfo.Invalid;
            WorkGiver_Scanner scannerWhoProvidedTarget = null;
            WorkGiver_Scanner scanner;
            IntVec3 pawnPosition;
            float closestDistSquared;
            float bestPriority;
            bool prioritized;
            bool allowUnreachable;
            Danger maxPathDanger;
            for (var j = 0; j < list.Count; j++)
            {
                var retries = 3;
                var allowRetry = true;
                while (allowRetry && retries > 0)
                {
                    allowRetry = false;
                    var workGiver = list[j];
                    if (workGiver.def.priorityInType != num && bestTargetOfLastPriority.IsValid) break;
                    if (!__instance.PawnCanUseWorkGiver(pawn, workGiver)) continue;
                    var potentialWorkThingRequest = ThingRequest.ForUndefined();
                    try
                    {
                        var job2 = workGiver.NonScanJob(pawn);
                        if (job2 != null)
                        {
                            __result = new ThinkResult(job2, __instance, workGiver.def.tagToGive);
#if DEBUG
                            s1.Stop();
                            if (Prefs.LogVerbose && s1.ElapsedMilliseconds > 50)
                            {
                                Log.Warning(pawn.ToString() + " " + __instance.ToString() + " NonScanJob JobGiver_Work.TryIssueJobPackage Took over " + s1.ElapsedMilliseconds.ToString() + "ms");
                            }
#endif
                            return false;
                        }

                        scanner = workGiver as WorkGiver_Scanner;

                        if (scanner != null)
                        {
                            if (scanner.def.scanThings)
                            {
                                potentialWorkThingRequest = ((WorkGiver_Scanner) workGiver).PotentialWorkThingRequest;
                                Predicate<Thing> validator;
                                if (scanner is WorkGiver_DoBill workGiver_DoBill)
                                    validator = t =>
                                        !t.IsForbidden(pawn) &&
                                        WorkGiver_Scanner_Patch.HasJobOnThing(workGiver_DoBill, pawn, t);
                                else
                                    validator = t => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);
                                var enumerable = scanner.PotentialWorkThingsGlobal(pawn);
                                Thing thing;
                                if (scanner.Prioritized)
                                {
                                    var enumerable2 = enumerable;
                                    if (enumerable2 == null)
                                        enumerable2 =
                                            pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    thing = !scanner.AllowUnreachable
                                        ? GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, enumerable2,
                                            scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)),
                                            9999f, validator, x => scanner.GetPriority(pawn, x))
                                        : GenClosest.ClosestThing_Global(pawn.Position, enumerable2, 99999f, validator,
                                            x => scanner.GetPriority(pawn, x));
                                }
                                else if (scanner.AllowUnreachable)
                                {
                                    var enumerable3 = enumerable;
                                    if (enumerable3 == null)
                                        enumerable3 =
                                            pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                    thing = GenClosest.ClosestThing_Global(pawn.Position, enumerable3, 99999f,
                                        validator);
                                }
                                else
                                {
                                    if (workGiver.def == haulGeneral)
                                        thing = HaulingCache.ClosestThingReachable(pawn, scanner, pawn.Map,
                                            scanner.PotentialWorkThingRequest, scanner.PathEndMode,
                                            TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator,
                                            enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                            enumerable != null);
                                    else if (scanner.PotentialWorkThingRequest.singleDef == null &&
                                             thingRequestGroupsCached.Contains(scanner.PotentialWorkThingRequest.group))
                                        thing = GenClosest_Patch.ClosestThingRequestGroup(pawn, scanner, pawn.Map,
                                            scanner.PotentialWorkThingRequest, scanner.PathEndMode,
                                            TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator,
                                            enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                            enumerable != null);
                                    else if (workGiversForClosestThingReachable2.Contains(workGiver.def))
                                        thing = GenClosest_Patch.ClosestThingReachable2(pawn.Position, pawn.Map,
                                            scanner.PotentialWorkThingRequest, scanner.PathEndMode,
                                            TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator,
                                            enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                            enumerable != null);
                                    else
                                        thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                                            scanner.PotentialWorkThingRequest, scanner.PathEndMode,
                                            TraverseParms.For(pawn, scanner.MaxPathDanger(pawn)), 9999f, validator,
                                            enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch,
                                            enumerable != null);
                                }

                                if (thing != null)
                                {
                                    bestTargetOfLastPriority = thing;
                                    scannerWhoProvidedTarget = scanner;
                                }
                            }

                            if (scanner.def.scanCells)
                            {
                                pawnPosition = pawn.Position;
                                closestDistSquared = 99999f;
                                bestPriority = float.MinValue;
                                prioritized = scanner.Prioritized;
                                allowUnreachable = scanner.AllowUnreachable;
                                maxPathDanger = scanner.MaxPathDanger(pawn);
                                IEnumerable<IntVec3> enumerable4;
                                if (scanner is WorkGiver_GrowerSow workGiver_Grower)
                                {
                                    var bestCell =
                                        WorkGiver_Grower_Patch.ClosestLocationReachable(workGiver_Grower, pawn);
                                    if (bestCell.IsValid)
                                    {
                                        bestTargetOfLastPriority = new TargetInfo(bestCell, pawn.Map);
                                        scannerWhoProvidedTarget = scanner;
                                    }
                                }
                                else if (scanner is WorkGiver_GrowerHarvest workGiver_GrowerHarvest)
                                {
                                    var bestCell =
                                        WorkGiver_GrowerHarvest_Patch.ClosestLocationReachable(workGiver_GrowerHarvest,
                                            pawn);
                                    if (bestCell.IsValid)
                                    {
                                        bestTargetOfLastPriority = new TargetInfo(bestCell, pawn.Map);
                                        scannerWhoProvidedTarget = scanner;
                                    }
                                }
                                else
                                {
                                    enumerable4 = scanner.PotentialWorkCellsGlobal(pawn);
                                    IList<IntVec3> list2;
                                    if ((list2 = enumerable4 as IList<IntVec3>) != null)
                                        for (var k = 0; k < list2.Count; k++)
                                            ProcessCell(list2[k]);
                                    else
                                        foreach (var item in enumerable4)
                                            ProcessCell(item);
                                }
                            }
                        }

                        void ProcessCell(IntVec3 c)
                        {
                            float newDistanceSquared = (c - pawnPosition).LengthHorizontalSquared;
                            var newPriority = 0f;

                            if (prioritized)
                            {
                                newPriority = scanner.GetPriority(pawn, c);
                                if (newPriority < bestPriority) return;
                            }

                            if (newDistanceSquared < closestDistSquared && !c.IsForbidden(pawn) &&
                                scanner.HasJobOnCell(pawn, c))
                            {
                                if (!allowUnreachable && !pawn.CanReach(c, scanner.PathEndMode, maxPathDanger)) return;

                                bestTargetOfLastPriority = new TargetInfo(c, pawn.Map);
                                scannerWhoProvidedTarget = scanner;
                                closestDistSquared = newDistanceSquared;
                                bestPriority = newPriority;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Concat(pawn, " threw exception in WorkGiver ", workGiver.def.defName, ": ",
                            ex.ToString()));
                    }

                    if (bestTargetOfLastPriority.IsValid)
                    {
                        var job3 = !bestTargetOfLastPriority.HasThing
                            ? scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell)
                            : scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing);
                        if (job3 != null)
                        {
                            job3.workGiverDef = scannerWhoProvidedTarget.def;
                            __result = new ThinkResult(job3, __instance, workGiver.def.tagToGive);

#if DEBUG
                            s2.Stop();
                            if (Prefs.LogVerbose && s2.ElapsedMilliseconds > 50)
                            {
                                Log.Warning(pawn.ToString() + " JobGiver_Work.TryIssueJobPackage Took over " + s2.ElapsedMilliseconds.ToString() + "ms for workGiver: " + workGiver.def.defName + ":" + potentialWorkThingRequest);
                                //Log.Warning(scanner.PotentialWorkThingRequest.ToString());
                                //Log.Warning(validator.ToString());
                            }
                            s1.Stop();
                            if (Prefs.LogVerbose && s1.ElapsedMilliseconds > 50)
                            {
                                Log.Warning(pawn.ToString() + " " + __instance.ToString() + " bestTargetOfLastPriority JobGiver_Work.TryIssueJobPackage Took over " + s2.ElapsedMilliseconds.ToString() + "ms");
                                //Log.Warning(scanner.PotentialWorkThingRequest.ToString());
                                //Log.Warning(validator.ToString());
                            }
#endif
                            return false;
                        }

                        //If this was a cached plant job, deregister it and check if it is still valid to be registered
                        if (scannerWhoProvidedTarget is WorkGiver_GrowerSow)
                        {
                            var map = pawn.Map;
                            var cell = bestTargetOfLastPriority.Cell;
                            JumboCell.ReregisterObject(map, cell, RimThreaded.plantSowing_Cache);
                        }
                        //HACK - I know. I'm awful.
                        //Log.ErrorOnce(string.Concat(scannerWhoProvidedTarget, " provided target ", bestTargetOfLastPriority, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."), 6112651);

                        if (retries <= 1)
                            if (Prefs.LogVerbose)
                                Log.Warning(string.Concat(scannerWhoProvidedTarget, " provided target ",
                                    bestTargetOfLastPriority, " but yielded no actual job for pawn ", pawn,
                                    ". The CanGiveJob and JobOnX methods may not be synchronized."));
                        allowRetry = true;
                    }

                    num = workGiver.def.priorityInType;

                    retries--;
                }
            }

            __result = ThinkResult.NoJob;
            return false;
        }

        private static Job GiverTryGiveJobPrioritized(JobGiver_Work __instance, Pawn pawn, WorkGiver giver,
            IntVec3 cell)
        {
            if (!__instance.PawnCanUseWorkGiver(pawn, giver)) return null;
            try
            {
                var job = giver.NonScanJob(pawn);
                if (job != null) return job;
                var scanner = giver as WorkGiver_Scanner;
                if (scanner != null)
                {
                    if (giver.def.scanThings)
                    {
                        Predicate<Thing> predicate;
                        if (scanner is WorkGiver_DoBill workGiver_DoBill)
                            predicate = t =>
                                !t.IsForbidden(pawn) &&
                                WorkGiver_Scanner_Patch.HasJobOnThing(workGiver_DoBill, pawn, t);
                        else
                            predicate = t => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);

                        var thingList = cell.GetThingList(pawn.Map);
                        for (var i = 0; i < thingList.Count; i++)
                        {
                            var thing = thingList[i];
                            if (scanner.PotentialWorkThingRequest.Accepts(thing) && predicate(thing))
                            {
                                var job2 = scanner.JobOnThing(pawn, thing);
                                if (job2 != null) job2.workGiverDef = giver.def;
                                return job2;
                            }
                        }
                    }

                    if (giver.def.scanCells && !cell.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, cell))
                    {
                        var job3 = scanner.JobOnCell(pawn, cell);
                        if (job3 != null) job3.workGiverDef = giver.def;
                        return job3;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(string.Concat(pawn, " threw exception in GiverTryGiveJobTargeted on WorkGiver ",
                    giver.def.defName, ": ", ex.ToString()));
            }

            return null;
        }
    }
}