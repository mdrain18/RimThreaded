using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class RecipeWorkerCounter_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(RecipeWorkerCounter);
            var patched = typeof(RecipeWorkerCounter_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetCarriedCount");
        }

        public static bool GetCarriedCount(RecipeWorkerCounter __instance, ref int __result, Bill_Production bill,
            ThingDef prodDef)
        {
            var num = 0;
            //foreach (Pawn item in bill.Map.mapPawns.FreeColonistsSpawned)
            if (!RimThreaded.billFreeColonistsSpawned.TryGetValue(bill, out var freeColonistsSpawned))
            {
                freeColonistsSpawned = bill.Map.mapPawns.FreeColonistsSpawned;
                RimThreaded.billFreeColonistsSpawned[bill] = freeColonistsSpawned;
            }

            for (var i = 0; i < freeColonistsSpawned.Count; i++)
            {
                var carriedThing = freeColonistsSpawned[i]?.carryTracker?.CarriedThing;
                if (carriedThing == null) continue;
                var stackCount = carriedThing.stackCount;
                carriedThing = carriedThing.GetInnerIfMinified();
                if (__instance.CountValidThing(carriedThing, bill, prodDef)) num += stackCount;
            }

            __result = num;
            return false;
        }
    }
}