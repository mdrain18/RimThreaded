using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    internal class Pawn_PlayerSettings_Patch
    {
        public static Dictionary<Pawn, List<Pawn>> pets = new Dictionary<Pawn, List<Pawn>>();
        public static bool petsInit;

        public static void RunDestructivePatches()
        {
            var original = typeof(Pawn_PlayerSettings);
            var patched = typeof(Pawn_PlayerSettings_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(set_Master));
        }

        public static bool set_Master(Pawn_PlayerSettings __instance, Pawn value)
        {
            if (__instance.master == value) return false;

            if (petsInit == false) RebuildPetsDictionary();

            var flag = ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(__instance.pawn);
            if (__instance.master != null)
                if (pets.TryGetValue(__instance.master, out var pawnList2))
                    pawnList2.Remove(__instance.pawn);
            __instance.master = null;
            if (value != null && !__instance.pawn.training.HasLearned(TrainableDefOf.Obedience))
            {
                Log.ErrorOnce("Attempted to set master for non-obedient pawn", 73908573);
                return false;
            }

            __instance.master = value;
            if (value != null)
            {
                if (!pets.TryGetValue(value, out var pawnList))
                {
                    pawnList = new List<Pawn>();
                    lock (pets)
                    {
                        pets[value] = pawnList;
                    }
                }

                pawnList.Add(__instance.pawn);
            }

            if (__instance.pawn.Spawned &&
                (flag || ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(__instance.pawn)))
                __instance.pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            return false;
        }

        public static void RebuildPetsDictionary()
        {
            lock (pets)
            {
                if (petsInit == false)
                {
                    for (var i = 0; i < PawnsFinder.AllMapsWorldAndTemporary_Alive.Count; i++)
                    {
                        var p = PawnsFinder.AllMapsWorldAndTemporary_Alive[i];
                        if (p.playerSettings != null)
                        {
                            var master = p.playerSettings.Master;
                            if (master != null)
                            {
                                if (!pets.TryGetValue(master, out var pawnList)) pawnList = new List<Pawn>();
                                pawnList.Add(p);
                            }
                        }
                    }

                    petsInit = true;
                }
            }
        }
    }
}