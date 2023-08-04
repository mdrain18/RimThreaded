using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class SeasonUtility_Patch
    {
        public static Dictionary<int, Dictionary<float, Season>> yearLatitudeSeason =
            new Dictionary<int, Dictionary<float, Season>>();

        internal static void RunDestructivePatches()
        {
            var original = typeof(SeasonUtility);
            var patched = typeof(SeasonUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetReportedSeason");
        }

        public static bool GetReportedSeason(ref Season __result, float yearPct, float latitude)
        {
            var year1000 = (int) (yearPct * 1000f);
            if (!yearLatitudeSeason.TryGetValue(year1000, out var latitudeSeason))
            {
                latitudeSeason = new Dictionary<float, Season>();
                yearLatitudeSeason.Add(year1000, latitudeSeason);
            }

            if (!latitudeSeason.TryGetValue(latitude, out var season))
            {
                SeasonUtility.GetSeason(yearPct, latitude, out var spring, out var summer, out var fall, out var winter,
                    out var permanentSummer, out var permanentWinter);
                if (permanentSummer == 1f) season = Season.PermanentSummer;

                if (permanentWinter == 1f) season = Season.PermanentWinter;
                if (permanentSummer != 1f && permanentWinter != 1f)
                    season = GenMath.MaxBy(Season.Spring, spring, Season.Summer, summer, Season.Fall, fall,
                        Season.Winter, winter);
                latitudeSeason.Add(latitude, season);
            }

            __result = season;
            return false;
        }
    }
}