using Verse;

namespace RimThreaded.RW_Patches
{
    internal class GenGrid_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            var original = typeof(GenGrid);
            var patched = typeof(GenGrid_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(InBounds), new[] {typeof(IntVec3), typeof(Map)}, false);
        }

        public static bool InBounds(ref bool __result, IntVec3 c, Map map)
        {
            if (map == null)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}