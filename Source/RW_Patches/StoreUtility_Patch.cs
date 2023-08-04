using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class StoreUtility_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(StoreUtility);
            var patched = typeof(StoreUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(CurrentHaulDestinationOf), new[] {typeof(Thing)});
        }

        public static bool CurrentHaulDestinationOf(ref IHaulDestination __result, Thing t)
        {
            __result = null;
            if (t == null)
                return false;
            if (!t.Spawned)
            {
                __result = t.ParentHolder as IHaulDestination;
                return false;
            }

            var map = t.Map;
            if (map == null)
                return false;
            var haulDestinationManager = map.haulDestinationManager;
            if (haulDestinationManager == null)
                return false;
            __result = haulDestinationManager.SlotGroupParentAt(t.Position);
            return false;
        }
    }
}