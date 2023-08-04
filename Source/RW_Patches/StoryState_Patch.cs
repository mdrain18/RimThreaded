using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class StoryState_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(StoryState);
            var patched = typeof(StoryState_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RecordPopulationIncrease");
        }

        public static bool RecordPopulationIncrease(StoryState __instance)
        {
            var count = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists.Count;
            lock (__instance.colonistCountTicks)
            {
                if (!__instance.colonistCountTicks.ContainsKey(count))
                    __instance.colonistCountTicks.Add(count, Find.TickManager.TicksGame);
            }

            return false;
        }
    }
}