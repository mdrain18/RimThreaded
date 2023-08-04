using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    public class WorkGiver_DoBill_RegionProcessor
    {
        public int adjacentRegionsAvailable;
        public Predicate<Thing> baseValidator;
        public Bill bill;
        public bool billGiverIsPawn;
        public List<ThingCount> chosen;
        public bool foundAll;
        public List<IngredientCount> ingredientsOrdered = new List<IngredientCount>();
        public List<Thing> newRelevantThings = new List<Thing>();
        public Pawn pawn;
        public HashSet<Thing> processedThings = new HashSet<Thing>();
        public int regionsProcessed;
        public List<Thing> relevantThings = new List<Thing>();
        public IntVec3 rootCell;

        public bool Get_RegionProcessor(Region r)
        {
            var thingList = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
            for (var index = 0; index < thingList.Count; ++index)
            {
                var thing = thingList[index];
                if (!processedThings.Contains(thing) && ReachabilityWithinRegion.ThingFromRegionListerReachable(
                        thing, r, PathEndMode.ClosestTouch, pawn) && baseValidator(thing) &&
                    !(thing.def.IsMedicine & billGiverIsPawn))
                {
                    newRelevantThings.Add(thing);
                    processedThings.Add(thing);
                }
            }

            ++regionsProcessed;
            if (newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
            {
                relevantThings.AddRange(newRelevantThings);
                newRelevantThings.Clear();
                if (WorkGiver_DoBill_Patch.TryFindBestBillIngredientsInSet2(relevantThings, bill, chosen, rootCell,
                        billGiverIsPawn, ingredientsOrdered))
                {
                    foundAll = true;
                    return true;
                }
            }

            return false;
        }
    }
}