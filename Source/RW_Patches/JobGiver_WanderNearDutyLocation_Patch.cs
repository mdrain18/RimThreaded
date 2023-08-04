﻿using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    internal class JobGiver_WanderNearDutyLocation_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(JobGiver_WanderNearDutyLocation);
            var patched = typeof(JobGiver_WanderNearDutyLocation_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(GetWanderRoot));
        }

        public static bool GetWanderRoot(JobGiver_WanderNearDutyLocation __instance, ref IntVec3 __result, Pawn pawn)
        {
            __result = IntVec3.Invalid;
            var duty = pawn.mindState.duty;
            if (duty != null) __result = WanderUtility.BestCloseWanderRoot(duty.focus.Cell, pawn);
            return false;
        }
    }
}