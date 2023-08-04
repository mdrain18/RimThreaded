using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RimThreaded.RW_Patches
{
    internal class ThinkNode_JoinVoluntarilyJoinableLord_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(ThinkNode_JoinVoluntarilyJoinableLord);
            var patched = typeof(ThinkNode_JoinVoluntarilyJoinableLord_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(JoinVoluntarilyJoinableLord));
        }

        public static bool JoinVoluntarilyJoinableLord(ThinkNode_JoinVoluntarilyJoinableLord __instance, Pawn pawn)
        {
            var lord1 = pawn.GetLord();
            Lord lord2 = null;
            var num1 = 0.0f;
            if (lord1 != null)
            {
                if (!(lord1.LordJob is LordJob_VoluntarilyJoinable lordJob2))
                    return false;
                lord2 = lord1;
                num1 = lordJob2.VoluntaryJoinPriorityFor(pawn);
            }

            var map = pawn.Map; //changed
            if (map != null) //changed
            {
                var lords = map.lordManager.lords; //changed
                for (var index = 0; index < lords.Count; ++index)
                    if (lords[index].LordJob is LordJob_VoluntarilyJoinable lordJob4 &&
                        lords[index].CurLordToil.VoluntaryJoinDutyHookFor(pawn) == __instance.dutyHook)
                    {
                        var num2 = lordJob4.VoluntaryJoinPriorityFor(pawn);
                        if (num2 > 0.0 && (lord2 == null || num2 > (double) num1))
                        {
                            lord2 = lords[index];
                            num1 = num2;
                        }
                    }
            }

            if (lord2 == null || lord1 == lord2)
                return false;
            lord1?.Notify_PawnLost(pawn, PawnLostCondition.LeftVoluntarily);
            lord2.AddPawn(pawn);
            return false;
        }
    }
}