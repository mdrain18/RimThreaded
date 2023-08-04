using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    internal class RestUtility_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(RestUtility);
            var patched = typeof(RestUtility_Patch);
            //RimThreadedHarmony.Prefix(original, patched, nameof(CurrentBed), null, false);
            RimThreadedHarmony.Prefix(original, patched, nameof(FindBedFor),
                new[] {typeof(Pawn), typeof(Pawn), typeof(bool), typeof(bool), typeof(GuestStatus)});
        }

        public static bool CurrentBed(Pawn __instance, ref Building_Bed __result)
        {
            if (__instance == null)
            {
                __result = null;
                return false;
            }

            return true;
        }


        public static bool FindBedFor(ref Building_Bed __result, Pawn sleeper, Pawn traveler,
            bool checkSocialProperness, bool ignoreOtherReservations = false, GuestStatus? guestStatus = null)
        {
            var flag = false;
            if (sleeper.Ideo != null)
                foreach (var item in sleeper.Ideo.PreceptsListForReading)
                    if (item.def.prefersSlabBed)
                    {
                        flag = true;
                        break;
                    }

            var list = flag ? RestUtility.bedDefsBestToWorst_SlabBed_Medical : RestUtility.bedDefsBestToWorst_Medical;
            var list2 = flag
                ? RestUtility.bedDefsBestToWorst_SlabBed_RestEffectiveness
                : RestUtility.bedDefsBestToWorst_RestEffectiveness;

            if (HealthAIUtility.ShouldSeekMedicalRest(sleeper))
            {
                if (sleeper.InBed() && sleeper.CurrentBed().Medical && RestUtility.IsValidBedFor(sleeper.CurrentBed(),
                        sleeper, traveler, checkSocialProperness, false, ignoreOtherReservations, guestStatus))
                {
                    __result = sleeper.CurrentBed();
                    return false;
                }

                for (var i = 0; i < list.Count; i++)
                {
                    var thingDef = list[i];
                    if (!RestUtility.CanUseBedEver(sleeper, thingDef)) continue;
                    for (var j = 0; j < 2; j++)
                    {
                        var maxDanger2 = j == 0 ? Danger.None : Danger.Deadly;
                        var building_Bed = (Building_Bed) GenClosest_Patch.ClosestBedReachable(sleeper.Position,
                            sleeper.Map, ThingRequest.ForDef(thingDef), PathEndMode.OnCell, TraverseParms.For(traveler),
                            9999f,
                            b => ((Building_Bed) b).Medical &&
                                 (int) b.Position.GetDangerFor(sleeper, sleeper.Map) <= (int) maxDanger2 &&
                                 RestUtility.IsValidBedFor(b, sleeper, traveler, checkSocialProperness, false,
                                     ignoreOtherReservations, guestStatus));
                        if (building_Bed != null)
                        {
                            __result = building_Bed;
                            return false;
                        }
                    }
                }
            }

            if (sleeper.RaceProps.Dryad)
            {
                __result = null;
                return false;
            }

            if (sleeper.ownership != null && sleeper.ownership.OwnedBed != null && RestUtility.IsValidBedFor(
                    sleeper.ownership.OwnedBed, sleeper, traveler, checkSocialProperness, false,
                    ignoreOtherReservations, guestStatus))
            {
                __result = sleeper.ownership.OwnedBed;
                return false;
            }

            var directPawnRelation = LovePartnerRelationUtility.ExistingMostLikedLovePartnerRel(sleeper, false);

            if (directPawnRelation != null)
            {
                var ownedBed = directPawnRelation.otherPawn.ownership.OwnedBed;
                if (ownedBed != null && RestUtility.IsValidBedFor(ownedBed, sleeper, traveler, checkSocialProperness,
                        false, ignoreOtherReservations, guestStatus))
                {
                    __result = ownedBed;
                    return false;
                }
            }

            for (var k = 0; k < 2; k++)
            {
                var maxDanger = k == 0 ? Danger.None : Danger.Deadly;
                for (var l = 0; l < list2.Count; l++)
                {
                    var thingDef2 = list2[l];
                    if (RestUtility.CanUseBedEver(sleeper, thingDef2))
                    {
                        var building_Bed2 = (Building_Bed) GenClosest_Patch.ClosestBedReachable(sleeper.Position,
                            sleeper.Map, ThingRequest.ForDef(thingDef2), PathEndMode.OnCell,
                            TraverseParms.For(traveler), 9999f,
                            b => !((Building_Bed) b).Medical &&
                                 (int) b.Position.GetDangerFor(sleeper, sleeper.Map) <= (int) maxDanger &&
                                 RestUtility.IsValidBedFor(b, sleeper, traveler, checkSocialProperness, false,
                                     ignoreOtherReservations, guestStatus));
                        if (building_Bed2 != null)
                        {
                            __result = building_Bed2;
                            return false;
                        }
                    }
                }
            }

            __result = null;
            return false;
        }
    }
}