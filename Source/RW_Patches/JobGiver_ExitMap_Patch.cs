using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    internal class JobGiver_ExitMap_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(JobGiver_ExitMap);
            var patched = typeof(JobGiver_ExitMap_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryGiveJob));
        }

        public static bool TryGiveJob(JobGiver_ExitMap __instance, ref Job __result, Pawn pawn)
        {
            var mindState = pawn.mindState;
            var canDig = __instance.forceCanDig ||
                         pawn.mindState.duty != null && pawn.mindState.duty.canDig && !pawn.CanReachMapEdge() ||
                         __instance.forceCanDigIfCantReachMapEdge && !pawn.CanReachMapEdge() ||
                         __instance.forceCanDigIfAnyHostileActiveThreat && pawn.Faction != null &&
                         GenHostility.AnyHostileActiveThreatTo(pawn.Map, pawn.Faction, true);
            IntVec3 dest;
            if (!__instance.TryFindGoodExitDest(pawn, canDig, __instance.canBash, out dest))
            {
                __result = null;
                return false;
            }

            if (canDig)
                using (var path = pawn.Map.pathFinder.FindPath(pawn.Position, dest,
                           TraverseParms.For(pawn, mode: TraverseMode.PassAllDestroyableThings)))
                {
                    IntVec3 cellBefore;
                    var blocker = path.FirstBlockingBuilding(out cellBefore, pawn);
                    if (blocker != null)
                    {
                        var job = DigUtility.PassBlockerJob(pawn, blocker, cellBefore, true, true);
                        if (job != null)
                        {
                            __result = job;
                            return false;
                        }
                    }
                }

            var job1 = JobMaker.MakeJob(JobDefOf.Goto, dest);
            job1.exitMapOnArrival = true;
            job1.failIfCantJoinOrCreateCaravan = __instance.failIfCantJoinOrCreateCaravan;
            job1.locomotionUrgency =
                PawnUtility.ResolveLocomotion(pawn, __instance.defaultLocomotion, LocomotionUrgency.Jog);
            job1.expiryInterval = __instance.jobMaxDuration;
            job1.canBashDoors = __instance.canBash;
            __result = job1;
            return false;
        }
    }
}