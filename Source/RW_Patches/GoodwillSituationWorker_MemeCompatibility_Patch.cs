using RimWorld;

namespace RimThreaded.RW_Patches
{
    internal class GoodwillSituationWorker_MemeCompatibility_Patch
    {
        public static void RunDestructivePatches()
        {
            var original = typeof(GoodwillSituationWorker_MemeCompatibility);
            var patched = typeof(GoodwillSituationWorker_MemeCompatibility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(Applies), new[] {typeof(Faction), typeof(Faction)});
        }

        public static bool Applies(GoodwillSituationWorker_MemeCompatibility __instance, ref bool __result, Faction a,
            Faction b)
        {
            var primaryIdeo1 = a.ideos.PrimaryIdeo;
            if (primaryIdeo1 == null)
                return false;
            var def = __instance.def;
            if (def == null)
            {
                __result = false;
                return false;
            }

            if (def.versusAll)
                return primaryIdeo1.memes.Contains(def.meme);
            var primaryIdeo2 = b.ideos.PrimaryIdeo;
            return primaryIdeo2 != null && primaryIdeo1.memes.Contains(def.meme) &&
                   primaryIdeo2.memes.Contains(def.otherMeme);
        }
    }
}