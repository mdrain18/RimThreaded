using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class Alert_ColonistLeftUnburied_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(Alert_ColonistLeftUnburied);
            var patched = typeof(Alert_ColonistLeftUnburied_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(IsCorpseOfColonist));
        }

        public static bool IsCorpseOfColonist(ref bool __result, Corpse corpse)
        {
            if (corpse == null)
            {
                __result = false;
                return false;
            }

            var InnerPawn = corpse.InnerPawn;
            if (InnerPawn == null)
            {
                __result = false;
                return false;
            }

            var def = InnerPawn.def;
            if (def == null)
            {
                __result = false;
                return false;
            }

            var race = def.race;
            if (race == null)
            {
                __result = false;
                return false;
            }

            __result = InnerPawn.Faction == Faction.OfPlayer && race.Humanlike && !InnerPawn.IsQuestLodger() &&
                       !InnerPawn.IsSlave && !corpse.IsInAnyStorage();
            return false;
        }
    }
}