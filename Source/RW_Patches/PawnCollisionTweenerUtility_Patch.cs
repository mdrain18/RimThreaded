using System.Reflection;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class PawnCollisionTweenerUtility_Patch
    {
        public static MethodInfo willBeFasterOnNextCell =
            typeof(PawnCollisionTweenerUtility).GetMethod("WillBeFasterOnNextCell",
                BindingFlags.Static | BindingFlags.NonPublic);

        internal static void RunDestructivePatches()
        {
            var original = typeof(PawnCollisionTweenerUtility);
            var patched = typeof(PawnCollisionTweenerUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetPawnsStandingAtOrAboutToStandAt");
            RimThreadedHarmony.Prefix(original, patched, "CanGoDirectlyToNextCell");
        }

        public static bool GetPawnsStandingAtOrAboutToStandAt(
            IntVec3 at,
            Map map,
            out int pawnsCount,
            out int pawnsWithLowerIdCount,
            out bool forPawnFound,
            Pawn forPawn)
        {
            pawnsCount = 0;
            pawnsWithLowerIdCount = 0;
            forPawnFound = false;
            foreach (var c in CellRect.SingleCell(at).ExpandedBy(1))
            foreach (var thing in map.thingGrid.ThingsAt(c))
                if (thing is Pawn p && p.GetPosture() == PawnPosture.Standing)
                {
                    var path = p.pather;
                    if (null != path)
                    {
                        if (c != at)
                        {
                            if (!path.MovingNow || path.nextCell != path.Destination.Cell ||
                                path.Destination.Cell != at)
                                continue;
                        }
                        else if (path.MovingNow)
                        {
                            continue;
                        }
                    }

                    if (p == forPawn)
                        forPawnFound = true;
                    ++pawnsCount;
                    if (p.thingIDNumber < forPawn.thingIDNumber)
                        ++pawnsWithLowerIdCount;
                }

            return false;
        }

        public static bool CanGoDirectlyToNextCell(ref bool __result, Pawn pawn)
        {
            var nextCell = pawn.pather.nextCell;
            foreach (var c in CellRect.FromLimits(nextCell, pawn.Position).ExpandedBy(1))
                //if (c.InBounds(pawn.Map))
                //{
                //Thing[] thingList = c.GetThingList(pawn.Map).ToArray<Thing>();
                //for (int i = 0; i < thingList.Length; i++)
            foreach (var thing in pawn.Map.thingGrid.ThingsAt(c))
            {
                var pawn2 = thing as Pawn;
                if (pawn2 != null && pawn2 != pawn && pawn2.GetPosture() == PawnPosture.Standing)
                {
                    var pather1 = pawn2.pather;
                    if (null != pather1)
                    {
                        if (pawn2.pather.MovingNow)
                            if ((pawn2.Position == nextCell &&
                                 (bool) willBeFasterOnNextCell.Invoke(null, new object[] {pawn, pawn2}) ||
                                 pawn2.pather.nextCell == nextCell || pawn2.Position == pawn.Position ||
                                 pawn2.pather.nextCell == pawn.Position &&
                                 (bool) willBeFasterOnNextCell.Invoke(null, new object[] {pawn2, pawn})) &&
                                pawn2.thingIDNumber < pawn.thingIDNumber)
                            {
                                __result = false;
                                return false;
                            }
                    }
                    else if (pawn2.Position == pawn.Position || pawn2.Position == nextCell)
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            //}

            __result = true;
            return false;
        }
    }
}