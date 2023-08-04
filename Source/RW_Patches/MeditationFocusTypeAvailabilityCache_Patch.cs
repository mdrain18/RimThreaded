using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class MeditationFocusTypeAvailabilityCache_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(MeditationFocusTypeAvailabilityCache);
            var patched = typeof(MeditationFocusTypeAvailabilityCache_Patch);
            RimThreadedHarmony.Prefix(original, patched, "PawnCanUse");
            RimThreadedHarmony.Prefix(original, patched, "ClearFor");
        }

        public static bool PawnCanUse(ref bool __result, Pawn p, MeditationFocusDef type)
        {
            if (!MeditationFocusTypeAvailabilityCache.pawnCanUseMeditationTypeCached.ContainsKey(p))
                lock (MeditationFocusTypeAvailabilityCache.pawnCanUseMeditationTypeCached)
                {
                    MeditationFocusTypeAvailabilityCache.pawnCanUseMeditationTypeCached[p] =
                        new Dictionary<MeditationFocusDef, bool>();
                }

            if (!MeditationFocusTypeAvailabilityCache.pawnCanUseMeditationTypeCached[p].ContainsKey(type))
                lock (MeditationFocusTypeAvailabilityCache.pawnCanUseMeditationTypeCached[p])
                {
                    MeditationFocusTypeAvailabilityCache.pawnCanUseMeditationTypeCached[p][type] =
                        PawnCanUseInt(p, type);
                }

            __result = MeditationFocusTypeAvailabilityCache.pawnCanUseMeditationTypeCached[p][type];
            return false;
        }

        public static bool ClearFor(Pawn p)
        {
            if (MeditationFocusTypeAvailabilityCache.pawnCanUseMeditationTypeCached.ContainsKey(p))
                lock (MeditationFocusTypeAvailabilityCache.pawnCanUseMeditationTypeCached[p])
                {
                    MeditationFocusTypeAvailabilityCache.pawnCanUseMeditationTypeCached[p] =
                        new Dictionary<MeditationFocusDef, bool>();
                }

            return false;
        }

        private static bool PawnCanUseInt(Pawn p, MeditationFocusDef type)
        {
            if (p.story != null)
            {
                for (var i = 0; i < p.story.traits.allTraits.Count; i++)
                {
                    var disallowedMeditationFocusTypes =
                        p.story.traits.allTraits[i].CurrentData.disallowedMeditationFocusTypes;
                    if (disallowedMeditationFocusTypes != null && disallowedMeditationFocusTypes.Contains(type))
                        return false;
                }

                var list = p.story.adulthood?.spawnCategories;
                var list2 = p.story.childhood?.spawnCategories;
                for (var j = 0; j < type.incompatibleBackstoriesAny.Count; j++)
                {
                    var backstoryCategoryAndSlot = type.incompatibleBackstoriesAny[j];
                    var list3 = backstoryCategoryAndSlot.slot == BackstorySlot.Adulthood ? list : list2;
                    if (list3 != null && list3.Contains(backstoryCategoryAndSlot.categoryName)) return false;
                }
            }

            if (type.requiresRoyalTitle)
            {
                if (p.royalty != null)
                    return p.royalty.AllTitlesInEffectForReading.Any(t => t.def.allowDignifiedMeditationFocus);

                return false;
            }

            if (p.story != null)
            {
                for (var k = 0; k < p.story.traits.allTraits.Count; k++)
                {
                    var allowedMeditationFocusTypes =
                        p.story.traits.allTraits[k].CurrentData.allowedMeditationFocusTypes;
                    if (allowedMeditationFocusTypes != null && allowedMeditationFocusTypes.Contains(type)) return true;
                }

                var list4 = p.story.adulthood?.spawnCategories;
                var list5 = p.story.childhood?.spawnCategories;
                for (var l = 0; l < type.requiredBackstoriesAny.Count; l++)
                {
                    var backstoryCategoryAndSlot2 = type.requiredBackstoriesAny[l];
                    var list6 = backstoryCategoryAndSlot2.slot == BackstorySlot.Adulthood ? list4 : list5;
                    if (list6 != null && list6.Contains(backstoryCategoryAndSlot2.categoryName)) return true;
                }
            }

            if (type.requiredBackstoriesAny.Count == 0)
            {
                var flag = false;
                for (var m = 0; m < DefDatabase<TraitDef>.AllDefsListForReading.Count; m++)
                {
                    if (flag) break;

                    var traitDef = DefDatabase<TraitDef>.AllDefsListForReading[m];
                    for (var n = 0; n < traitDef.degreeDatas.Count; n++)
                    {
                        var allowedMeditationFocusTypes2 = traitDef.degreeDatas[n].allowedMeditationFocusTypes;
                        if (allowedMeditationFocusTypes2 != null && allowedMeditationFocusTypes2.Contains(type))
                        {
                            flag = true;
                            break;
                        }
                    }
                }

                if (!flag) return true;
            }

            return false;
        }
    }
}