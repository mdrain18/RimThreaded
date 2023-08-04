using System;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    internal class ThinkNode_ForbidOutsideFlagRadius_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(ThinkNode_ForbidOutsideFlagRadius);
            var patched = typeof(ThinkNode_ForbidOutsideFlagRadius_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryIssueJobPackage));
        }

        public static bool TryIssueJobPackage(ThinkNode_ForbidOutsideFlagRadius __instance, ref ThinkResult __result,
            Pawn pawn, JobIssueParams jobParams)
        {
            __result = ThinkResult.NoJob;
            if (pawn == null)
                return false;
            var mindState = pawn.mindState;
            if (mindState == null)
                return false;
            try
            {
                if (__instance.maxDistToSquadFlag > 0.0)
                {
                    if (mindState.maxDistToSquadFlag > 0.0)
                        Log.Error("Squad flag was not reset properly; raiders may behave strangely");
                    pawn.mindState.maxDistToSquadFlag = __instance.maxDistToSquadFlag;
                }

                __result = TryIssueJobPackage2(__instance, pawn, jobParams);
                return false;
            }
            finally
            {
                pawn.mindState.maxDistToSquadFlag = -1f;
            }
        }

        public static ThinkResult TryIssueJobPackage2(ThinkNode_Priority __instance, Pawn pawn,
            JobIssueParams jobParams)
        {
            var count = __instance.subNodes.Count;
            for (var index = 0; index < count; ++index)
            {
                var thinkResult = ThinkResult.NoJob;
                try
                {
                    var thinkNode = __instance.subNodes[index];
                    if (thinkNode == null)
                        return thinkResult;
                    thinkResult = thinkNode.TryIssueJobPackage(pawn, jobParams);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception in " + __instance.GetType() + " TryIssueJobPackage: " + ex);
                }

                if (thinkResult.IsValid)
                    return thinkResult;
            }

            return ThinkResult.NoJob;
        }
    }
}