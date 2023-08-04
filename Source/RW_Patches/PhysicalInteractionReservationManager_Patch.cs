using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    public class PhysicalInteractionReservationManager_Patch
    {
        public static
            Dictionary<PhysicalInteractionReservationManager, Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>>>
            instanceTargetToPawnToJob =
                new Dictionary<PhysicalInteractionReservationManager,
                    Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>>>();

        public static
            Dictionary<PhysicalInteractionReservationManager, Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>>>
            instancePawnToTargetToJob =
                new Dictionary<PhysicalInteractionReservationManager,
                    Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>>>();

        public static void RunDestructivePatches()
        {
            var original = typeof(PhysicalInteractionReservationManager);
            var patched = typeof(PhysicalInteractionReservationManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(IsReservedBy));
            RimThreadedHarmony.Prefix(original, patched, nameof(Reserve));
            RimThreadedHarmony.Prefix(original, patched, nameof(Release));
            RimThreadedHarmony.Prefix(original, patched, nameof(FirstReserverOf));
            RimThreadedHarmony.Prefix(original, patched, nameof(FirstReservationFor));
            RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseAllForTarget));
            RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseClaimedBy));
            RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseAllClaimedBy));
        }


        public static bool IsReservedBy(PhysicalInteractionReservationManager __instance, ref bool __result,
            Pawn claimant, LocalTargetInfo target)
        {
            __result = false;
            if (instanceTargetToPawnToJob.TryGetValue(__instance, out var targetToPawnToJob))
                if (targetToPawnToJob.TryGetValue(target, out var pawnToJob))
                    __result = pawnToJob.ContainsKey(claimant);
            return false;
        }

        public static bool Reserve(PhysicalInteractionReservationManager __instance, Pawn claimant, Job job,
            LocalTargetInfo target)
        {
            if (!instanceTargetToPawnToJob.TryGetValue(__instance, out var targetToPawnToJob))
                lock (__instance)
                {
                    if (!instanceTargetToPawnToJob.TryGetValue(__instance, out var targetToPawnToJob2))
                    {
                        targetToPawnToJob2 = new Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>>();
                        instanceTargetToPawnToJob.Add(__instance, targetToPawnToJob2);
                    }

                    targetToPawnToJob = targetToPawnToJob2;
                }

            if (!targetToPawnToJob.TryGetValue(target, out var pawnToJob))
                lock (__instance)
                {
                    if (!targetToPawnToJob.TryGetValue(target, out var pawnToJob2))
                    {
                        pawnToJob2 = new Dictionary<Pawn, Job>();
                        targetToPawnToJob.Add(target, pawnToJob2);
                    }

                    pawnToJob = pawnToJob2;
                }

            if (!instancePawnToTargetToJob.TryGetValue(__instance, out var pawnToTargetToJob))
                lock (__instance)
                {
                    if (!instancePawnToTargetToJob.TryGetValue(__instance, out var pawnToTargetToJob2))
                    {
                        pawnToTargetToJob2 = new Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>>();
                        instancePawnToTargetToJob.Add(__instance, pawnToTargetToJob2);
                    }

                    pawnToTargetToJob = pawnToTargetToJob2;
                }

            if (!pawnToTargetToJob.TryGetValue(claimant, out var targetToJob))
                lock (__instance)
                {
                    if (!pawnToTargetToJob.TryGetValue(claimant, out var targetToJob2))
                    {
                        targetToJob2 = new Dictionary<LocalTargetInfo, Job>();
                        pawnToTargetToJob.Add(claimant, targetToJob2);
                    }

                    targetToJob = targetToJob2;
                }

            lock (__instance)
            {
                pawnToJob.Add(claimant, job);
                targetToJob.Add(target, job);
            }

            JumboCell.ReregisterObject(claimant.Map, target.Cell, RimThreaded.plantHarvest_Cache);
            return false;
        }

        public static bool Release(PhysicalInteractionReservationManager __instance, Pawn claimant, Job job,
            LocalTargetInfo target)
        {
            var plantReregistered = false;
            lock (__instance)
            {
                if (instanceTargetToPawnToJob.TryGetValue(__instance, out var targetToPawnToJob))
                {
                    if (targetToPawnToJob.TryGetValue(target, out var pawnToJob))
                    {
                        if (pawnToJob.TryGetValue(claimant, out var outJob))
                        {
                            if (outJob == job)
                            {
                                pawnToJob.Remove(claimant);
                                plantReregistered = true;
                                JumboCell.ReregisterObject(claimant.Map, target.Cell, RimThreaded.plantHarvest_Cache);
                            }
                            else
                            {
                                Log.Warning(claimant + " tried to release reservation on target " + target +
                                            ", but job was different.");
                            }
                        }
                        else
                        {
                            Log.Warning(claimant + " tried to release reservation on target " + target +
                                        ", but job was different.");
                        }
                    }
                    else
                    {
                        Log.Warning(claimant + " tried to release reservation on target " + target +
                                    ", but claimant was not found.");
                    }
                }
                else
                {
                    Log.Warning(claimant + " tried to release reservation on target " + target +
                                ", but target had no physical reservations.");
                }

                if (instancePawnToTargetToJob.TryGetValue(__instance, out var pawnToTargetToJob))
                {
                    if (pawnToTargetToJob.TryGetValue(claimant, out var targetToJob))
                    {
                        if (targetToJob.TryGetValue(target, out var outJob2))
                        {
                            if (outJob2 == job)
                            {
                                var targetToJobResult = targetToJob.Remove(target);
                                if (!plantReregistered)
                                    JumboCell.ReregisterObject(claimant.Map, target.Cell,
                                        RimThreaded.plantHarvest_Cache);
                            }
                            else
                            {
                                Log.Warning(claimant + " tried to release reservation on target " + target +
                                            ", but job was different.");
                            }
                        }
                        else
                        {
                            Log.Warning(claimant + " tried to release reservation on target " + target +
                                        ", but job was different.");
                        }
                    }
                    else
                    {
                        Log.Warning(claimant + " tried to release reservation on target " + target +
                                    ", but claimant was not found.");
                    }
                }
                else
                {
                    Log.Warning(claimant + " tried to release reservation on target " + target +
                                ", but target had no physical reservations.");
                }
            }

            return false;
        }


        public static bool FirstReserverOf(PhysicalInteractionReservationManager __instance, ref Pawn __result,
            LocalTargetInfo target)
        {
            __result = null;
            if (instanceTargetToPawnToJob.TryGetValue(__instance, out var targetToPawnToJob))
                if (targetToPawnToJob.TryGetValue(target, out var pawnToJob) && pawnToJob.Count > 0)
                    try
                    {
                        __result = pawnToJob.First().Key;
                    }
                    catch (InvalidOperationException)
                    {
                    }

            return false;
        }

        public static bool FirstReservationFor(PhysicalInteractionReservationManager __instance,
            ref LocalTargetInfo __result, Pawn claimant)
        {
            __result = LocalTargetInfo.Invalid;
            if (instancePawnToTargetToJob.TryGetValue(__instance, out var pawnToTargetToJob))
                if (pawnToTargetToJob.TryGetValue(claimant, out var targetToJob) && targetToJob.Count > 0)
                    try
                    {
                        __result = targetToJob.First().Key;
                    }
                    catch (InvalidOperationException)
                    {
                    }

            return false;
        }

        public static bool ReleaseAllForTarget(PhysicalInteractionReservationManager __instance, LocalTargetInfo target)
        {
            if (instanceTargetToPawnToJob.TryGetValue(__instance, out var targetToPawnToJob))
                if (targetToPawnToJob.TryGetValue(target, out _))
                    lock (__instance)
                    {
                        if (targetToPawnToJob.TryGetValue(target, out var pawnToJob))
                        {
                            foreach (var kvp in pawnToJob)
                                if (instancePawnToTargetToJob.TryGetValue(__instance, out var pawnToTargetToJob))
                                {
                                    var pawn = kvp.Key;
                                    if (pawnToTargetToJob.TryGetValue(pawn, out var targetToJob))
                                        if (targetToJob.TryGetValue(target, out _))
                                            targetToJob.Remove(target);
                                }

                            targetToPawnToJob.Remove(target);
                            if (target != null && target.Thing != null && target.Thing.Map != null)
                                JumboCell.ReregisterObject(target.Thing.Map, target.Cell,
                                    RimThreaded.plantHarvest_Cache);
                        }
                    }

            return false;
        }

        public static bool ReleaseClaimedBy(PhysicalInteractionReservationManager __instance, Pawn claimant, Job job)
        {
            if (instancePawnToTargetToJob.TryGetValue(__instance, out var pawnToTargetToJob))
                if (pawnToTargetToJob.TryGetValue(claimant, out var targetToJob) && targetToJob.Count > 0)
                    lock (__instance)
                    {
                        foreach (var kvp in targetToJob.ToList())
                            if (kvp.Value == job)
                            {
                                var localTargetInfo = kvp.Key;
                                if (instanceTargetToPawnToJob.TryGetValue(__instance, out var targetToPawnToJob))
                                    if (targetToPawnToJob.TryGetValue(localTargetInfo, out var pawnToJob))
                                        if (pawnToJob.TryGetValue(claimant, out var job2))
                                            if (job == job2)
                                                pawnToJob.Remove(claimant);
                                targetToJob.Remove(localTargetInfo);
                                JumboCell.ReregisterObject(claimant.Map, localTargetInfo.Cell,
                                    RimThreaded.plantHarvest_Cache);
                            }
                    }

            return false;
        }

        public static bool ReleaseAllClaimedBy(PhysicalInteractionReservationManager __instance, Pawn claimant)
        {
            if (instancePawnToTargetToJob.TryGetValue(__instance, out var pawnToTargetToJob))
                if (pawnToTargetToJob.TryGetValue(claimant, out var targetToJob) && targetToJob.Count > 0)
                    lock (__instance)
                    {
                        foreach (var kvp in targetToJob)
                        {
                            var localTargetInfo = kvp.Key;
                            if (instanceTargetToPawnToJob.TryGetValue(__instance, out var targetToPawnToJob))
                                if (targetToPawnToJob.TryGetValue(localTargetInfo, out var pawnToJob))
                                    if (pawnToJob.TryGetValue(claimant, out _))
                                    {
                                        pawnToJob.Remove(claimant);
                                        JumboCell.ReregisterObject(claimant.Map, localTargetInfo.Cell,
                                            RimThreaded.plantHarvest_Cache);
                                    }
                        }

                        pawnToTargetToJob.Remove(claimant);
                    }

            return false;
        }
    }
}