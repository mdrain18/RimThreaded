using Verse;
using Verse.AI;

namespace RimThreaded
{
    internal class Patch_TryOpportunisticJob
    {
        public static Pawn getPawn(Pawn_JobTracker jobTracker)
        {
            return jobTracker.pawn;
        }
    }
}