using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{
    public class Pawn_RelationsTracker_Patch
    {
        public static ConcurrentStack<HashSet<Pawn>> pawnHashsetStack = new ConcurrentStack<HashSet<Pawn>>();
        public static ConcurrentStack<List<Pawn>> pawnListStack = new ConcurrentStack<List<Pawn>>();


        internal static void RunDestructivePatches()
        {
            var original = typeof(Pawn_RelationsTracker);
            var patched = typeof(Pawn_RelationsTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, "get_FamilyByBlood");
            RimThreadedHarmony.Prefix(original, patched, "get_RelatedPawns");
        }

        internal static void RunNonDestructivePatches()
        {
            var original = typeof(FocusStrengthOffset_GraveCorpseRelationship);
            var patched = typeof(Pawn_RelationsTracker_Patch);
            var pMethod = Method(patched, "ReplacePotentiallyRelatedPawns");
            RimThreadedHarmony.harmony.Patch(Method(original, "CanApply"), transpiler: new HarmonyMethod(pMethod));
            //Pawn_RelationsTracker.get_RelatedPawns
            original = TypeByName("RimWorld.Pawn_RelationsTracker+<get_RelatedPawns>d__30");
            RimThreadedHarmony.harmony.Patch(Method(original, "MoveNext"), transpiler: new HarmonyMethod(pMethod));
            //Pawn_RelationsTracker
            original = typeof(Pawn_RelationsTracker);
            //Pawn_RelationsTracker.Notify_PawnKilled
            RimThreadedHarmony.harmony.Patch(Method(original, "Notify_PawnKilled"),
                transpiler: new HarmonyMethod(pMethod));
            //Pawn_RelationsTracker.Notify_PawnSold
            RimThreadedHarmony.harmony.Patch(Method(original, "Notify_PawnSold"),
                transpiler: new HarmonyMethod(pMethod));
            //PawnDiedOrDownedThoughtsUtility.AppendThoughts_Relations
            original = typeof(PawnDiedOrDownedThoughtsUtility);
            pMethod = Method(patched, "ReplacePotentiallyRelatedPawns");
            RimThreadedHarmony.harmony.Patch(Method(original, "AppendThoughts_Relations"),
                transpiler: new HarmonyMethod(pMethod));
        }

        public static IEnumerable<CodeInstruction> ReplacePotentiallyRelatedPawns(
            IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            var matchesFound = new int[1];
            var instructionsList = instructions.ToList();
            var i = 0;
            while (i < instructionsList.Count)
            {
                var matchIndex = 0;
                if (
                    (instructionsList[i].opcode == OpCodes.Callvirt || instructionsList[i].opcode == OpCodes.Call) &&
                    (MethodInfo) instructionsList[i].operand ==
                    Method(typeof(Pawn_RelationsTracker), "get_PotentiallyRelatedPawns")
                )
                {
                    instructionsList[i].operand =
                        Method(typeof(Pawn_RelationsTracker_Patch), "get_PotentiallyRelatedPawns2");
                    matchesFound[matchIndex]++;
                }

                yield return instructionsList[i++];
            }

            for (var mIndex = 0; mIndex < matchesFound.Length; mIndex++)
                if (matchesFound[mIndex] < 1)
                    Log.Error("IL code instruction set " + mIndex + " not found");
        }

        public static IEnumerable<Pawn> get_PotentiallyRelatedPawns2(Pawn_RelationsTracker __instance)
        {
            if (!__instance.RelatedToAnyoneOrAnyoneRelatedToMe) yield break;

            var stack = new List<Pawn>();
            var visited = new HashSet<Pawn>();
            //stack = SimplePool<List<Pawn>>.Get();
            //visited = SimplePool<HashSet<Pawn>>.Get();
            stack.Add(__instance.pawn);
            visited.Add(__instance.pawn);
            while (stack.Any())
            {
                var p = stack[stack.Count - 1];
                stack.RemoveLast();
                if (p != __instance.pawn) yield return p;

                var directRel = p.relations.directRelations;
                for (var i = 0; i < directRel.Count; i++)
                {
                    var otherPawn = directRel[i].otherPawn;
                    if (!visited.Contains(otherPawn))
                    {
                        stack.Add(otherPawn);
                        visited.Add(otherPawn);
                    }
                }

                foreach (var item in p.relations.pawnsWithDirectRelationsWithMe)
                    if (!visited.Contains(item))
                    {
                        stack.Add(item);
                        visited.Add(item);
                    }
            }
        }


        public static void Notify_PawnKilled(Pawn_RelationsTracker __instance, DamageInfo? dinfo, Map mapBeforeDeath)
        {
            foreach (var potentiallyRelatedPawn in __instance.PotentiallyRelatedPawns)
                if (!potentiallyRelatedPawn.Dead && potentiallyRelatedPawn.needs.mood != null)
                    potentiallyRelatedPawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
            RemoveMySpouseMarriageRelatedThoughts(__instance);
            if (__instance.everSeenByPlayer && !PawnGenerator.IsBeingGenerated(__instance.pawn) &&
                !__instance.pawn.RaceProps.Animal)
                AffectBondedAnimalsOnMyDeath(__instance);
            __instance.Notify_FailedRescueQuest();
        }

        public static void AffectBondedAnimalsOnMyDeath(Pawn_RelationsTracker __instance)
        {
            var num1 = 0;
            Pawn pawn2 = null;
            var directRel = __instance.directRelations;
            for (var index = 0; index < directRel.Count; ++index)
                if (directRel[index].def == PawnRelationDefOf.Bond && directRel[index].otherPawn.Spawned)
                {
                    pawn2 = directRel[index].otherPawn;
                    ++num1;
                    var num2 = Rand.Value;
                    var stateDef = num2 >= 0.25
                        ? num2 >= 0.5 ? num2 >= 0.75 ? MentalStateDefOf.Manhunter : MentalStateDefOf.Berserk :
                        MentalStateDefOf.Wander_Psychotic
                        : MentalStateDefOf.Wander_Sad;
                    directRel[index].otherPawn.mindState.mentalStateHandler.TryStartMentalState(stateDef,
                        "MentalStateReason_BondedHumanDeath".Translate(__instance.pawn).Resolve(), true);
                }

            if (num1 == 1)
            {
                Messages.Message(
                    (pawn2.Name == null || pawn2.Name.Numerical
                        ? "MessageBondedAnimalMentalBreak".Translate(pawn2.LabelIndefinite(),
                            __instance.pawn.LabelShort, pawn2.Named("ANIMAL"), __instance.pawn.Named("HUMAN"))
                        : "MessageNamedBondedAnimalMentalBreak".Translate(pawn2.KindLabelIndefinite(),
                            pawn2.Name.ToStringShort, __instance.pawn.LabelShort, pawn2.Named("ANIMAL"),
                            __instance.pawn.Named("HUMAN"))).CapitalizeFirst(), pawn2, MessageTypeDefOf.ThreatSmall);
            }
            else
            {
                if (num1 <= 1)
                    return;
                Messages.Message(
                    "MessageBondedAnimalsMentalBreak".Translate(num1, __instance.pawn.LabelShort,
                        __instance.pawn.Named("HUMAN")), pawn2, MessageTypeDefOf.ThreatSmall);
            }
        }

        public static void RemoveMySpouseMarriageRelatedThoughts(Pawn_RelationsTracker __instance)
        {
            foreach (var spouse in __instance.pawn.GetSpouses(false))
            {
                if (spouse == null || spouse.Dead)
                    continue;
                var mood = spouse.needs.mood;
                if (mood == null)
                    continue;
                if (mood != null)
                {
                    var memories = mood.thoughts.memories;
                    memories.RemoveMemoriesOfDef(ThoughtDefOf.GotMarried);
                    memories.RemoveMemoriesOfDef(ThoughtDefOf.HoneymoonPhase);
                }
            }
        }

        public static bool get_FamilyByBlood(Pawn_RelationsTracker __instance, ref IEnumerable<Pawn> __result)
        {
            if (!__instance.canCacheFamilyByBlood)
            {
                __result = FamilyByBlood_Internal(__instance);
                return false;
            }

            if (!__instance.familyByBloodIsCached)
            {
                var cachedFamilyByBlood = new HashSet<Pawn>();
                foreach (var pawn in FamilyByBlood_Internal(__instance))
                    cachedFamilyByBlood.Add(pawn);
                __instance.familyByBloodIsCached = true;
                __instance.cachedFamilyByBlood = cachedFamilyByBlood;
            }

            __result = __instance.cachedFamilyByBlood;
            return false;
        }

        public static bool get_RelatedPawns(Pawn_RelationsTracker __instance, ref IEnumerable<Pawn> __result)
        {
            __result = RelatedPawns2(__instance);
            return false;
        }

        private static IEnumerable<Pawn> RelatedPawns2(Pawn_RelationsTracker __instance)
        {
            __instance.canCacheFamilyByBlood = true;
            __instance.familyByBloodIsCached = false;
            __instance.cachedFamilyByBlood = new HashSet<Pawn>();
            try
            {
                foreach (var potentiallyRelatedPawn in __instance.PotentiallyRelatedPawns)
                    if (__instance.familyByBloodIsCached &&
                        __instance.cachedFamilyByBlood.Contains(potentiallyRelatedPawn) ||
                        __instance.pawn.GetRelations(potentiallyRelatedPawn).Any())
                        yield return potentiallyRelatedPawn;
            }
            finally
            {
                __instance.canCacheFamilyByBlood = false;
                __instance.familyByBloodIsCached = false;
                __instance.cachedFamilyByBlood = new HashSet<Pawn>();
            }
        }

        private static IEnumerable<Pawn> FamilyByBlood_Internal(Pawn_RelationsTracker __instance)
        {
            if (__instance.RelatedToAnyoneOrAnyoneRelatedToMe)
            {
                List<Pawn> familyStack = null;
                List<Pawn> familyChildrenStack = null;
                HashSet<Pawn> familyVisited = null;
                try
                {
                    //familyStack = SimplePool<List<Pawn>>.Get();
                    if (!pawnListStack.TryPop(out familyStack))
                        familyStack = new List<Pawn>();
                    //familyChildrenStack = SimplePool<List<Pawn>>.Get();
                    if (!pawnListStack.TryPop(out familyChildrenStack))
                        familyChildrenStack = new List<Pawn>();
                    //familyVisited = SimplePool<HashSet<Pawn>>.Get();
                    if (!pawnHashsetStack.TryPop(out familyVisited))
                        familyVisited = new HashSet<Pawn>();
                    familyStack.Add(__instance.pawn);
                    familyVisited.Add(__instance.pawn);
                    while (familyStack.Any())
                    {
                        var p = familyStack[familyStack.Count - 1];
                        familyStack.RemoveLast();
                        if (p != __instance.pawn)
                            yield return p;
                        var father = p.GetFather();
                        if (father != null && !familyVisited.Contains(father))
                        {
                            familyStack.Add(father);
                            familyVisited.Add(father);
                        }

                        var mother = p.GetMother();
                        if (mother != null && !familyVisited.Contains(mother))
                        {
                            familyStack.Add(mother);
                            familyVisited.Add(mother);
                        }

                        familyChildrenStack.Clear();
                        familyChildrenStack.Add(p);
                        while (familyChildrenStack.Any())
                        {
                            var child = familyChildrenStack[familyChildrenStack.Count - 1];
                            familyChildrenStack.RemoveLast();
                            if (child != p && child != __instance.pawn)
                                yield return child;
                            foreach (var child1 in child.relations.Children)
                                if (!familyVisited.Contains(child1))
                                {
                                    familyChildrenStack.Add(child1);
                                    familyVisited.Add(child1);
                                }

                            child = null;
                        }

                        p = null;
                    }
                }
                finally
                {
                    familyStack.Clear();
                    //SimplePool<List<Pawn>>.Return(familyStack);
                    pawnListStack.Push(familyStack);
                    familyChildrenStack.Clear();
                    //SimplePool<List<Pawn>>.Return(familyChildrenStack);
                    pawnListStack.Push(familyChildrenStack);
                    familyVisited.Clear();
                    //SimplePool<HashSet<Pawn>>.Return(familyVisited);
                    pawnHashsetStack.Push(familyVisited);
                }
            }
        }
    }
}