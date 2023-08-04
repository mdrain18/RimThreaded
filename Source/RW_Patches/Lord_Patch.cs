using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace RimThreaded.RW_Patches
{
    [StaticConstructorOnStartup]
    internal class Lord_Patch
    {
        public static Dictionary<Pawn, Lord> pawnsLord = new Dictionary<Pawn, Lord>();

        internal static void RunDestructivePatches()
        {
            var original = typeof(Lord);
            var patched = typeof(Lord_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(AddPawn));
            RimThreadedHarmony.Prefix(original, patched, nameof(AddPawns));
            RimThreadedHarmony.Prefix(original, patched, nameof(RemovePawn));
        }

        public static bool AddPawns(Lord __instance, IEnumerable<Pawn> pawns)
        {
            foreach (var pawn in pawns) AddPawn(__instance, pawn);
            return false;
        }

        public static bool AddPawn(Lord __instance, Pawn p)
        {
            if (__instance.ownedPawns.Contains(p))
            {
                Log.Error(string.Concat("Lord for ", __instance.faction.ToStringSafe(), " tried to add ", p,
                    " whom it already controls."));
            }
            else if (p.GetLord() != null)
            {
                Log.Error(string.Concat("Tried to add pawn ", p, " to lord ", __instance,
                    " but this pawn is already a member of lord ", p.GetLord(),
                    ". Pawns can't be members of more than one lord at the same time."));
            }
            else
            {
                lock (__instance.ownedPawns)
                {
                    __instance.ownedPawns.Add(p);
                }

                lock (pawnsLord)
                {
                    pawnsLord[p] = __instance;
                }

                __instance.numPawnsEverGained++;
                __instance.Map.attackTargetsCache.UpdateTarget(p);
                __instance.curLordToil.UpdateAllDuties();
                __instance.curJob.Notify_PawnAdded(p);
            }

            return false;
        }

        public static bool RemovePawn(Lord __instance, Pawn p)
        {
            lock (__instance.ownedPawns)
            {
                __instance.ownedPawns.Remove(p);
            }

            lock (pawnsLord)
            {
                pawnsLord[p] = null;
            }

            if (p.mindState != null) p.mindState.duty = null;

            __instance.Map.attackTargetsCache.UpdateTarget(p);
            return false;
        }
    }
}