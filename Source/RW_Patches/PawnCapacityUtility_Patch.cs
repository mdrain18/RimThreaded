using System.Collections.Generic;
using Verse;
using static Verse.PawnCapacityUtility;

namespace RimThreaded.RW_Patches
{
    public class PawnCapacityUtility_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            var original = typeof(PawnCapacityUtility);
            var patched = typeof(PawnCapacityUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(CalculatePartEfficiency), null, false);
        }

        public static bool CalculatePartEfficiency(ref float __result, HediffSet diffSet, BodyPartRecord part,
            bool ignoreAddedParts = false, List<CapacityImpactor> impactors = null)
        {
            if (part == null)
            {
                __result = 0f;
                return false;
            }

            return true;
        }
    }
}