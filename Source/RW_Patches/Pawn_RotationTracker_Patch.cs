using Verse;

namespace RimThreaded.RW_Patches
{
    internal class Pawn_RotationTracker_Patch
    {
        public static void RunDestructivePatches()
        {
            var original = typeof(Pawn_RotationTracker);
            var patched = typeof(Pawn_RotationTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(UpdateRotation));
        }

        public static bool UpdateRotation(Pawn_RotationTracker __instance)
        {
            if (__instance.pawn.Destroyed || __instance.pawn.jobs.HandlingFacing) return false;
            var stance_Busy = __instance.pawn.stances.curStance as Stance_Busy;
            if (stance_Busy != null && stance_Busy.focusTarg.IsValid)
            {
                if (stance_Busy.focusTarg.HasThing)
                    __instance.Face(stance_Busy.focusTarg.Thing.DrawPos);
                else
                    __instance.FaceCell(stance_Busy.focusTarg.Cell);
                return false;
            }

            if (__instance.pawn.pather.Moving)
            {
                if (__instance.pawn.pather.curPath != null && __instance.pawn.pather.curPath.NodesLeftCount >= 1)
                    __instance.FaceAdjacentCell(__instance.pawn.pather.nextCell);
                return false;
            }

            var curJob = __instance.pawn.CurJob;
            var curDriver = __instance.pawn.jobs.curDriver;
            if (curJob != null && curDriver != null && __instance.pawn.jobs.curJob != null)
            {
                var target = curJob.GetTarget(curDriver.rotateToFace);
                __instance.FaceTarget(target);
            }

            if (__instance.pawn.Drafted) __instance.pawn.Rotation = Rot4.South;
            return false;
        }
    }
}