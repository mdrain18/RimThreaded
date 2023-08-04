using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class DateNotifier_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(DateNotifier);
            var patched = typeof(DateNotifier_Patch);
            RimThreadedHarmony.Prefix(original, patched, "FindPlayerHomeWithMinTimezone");
        }

        public static bool FindPlayerHomeWithMinTimezone(DateNotifier __instance, ref Map __result)
        {
            var maps = Find.Maps;
            var map = maps[0];
            var num = -1;
            if (maps.Count > 1)
                for (var i = 0; i < maps.Count; i++)
                {
                    if (!maps[i].IsPlayerHome) continue;
                    var num2 = GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(maps[i].Tile).x);
                    if (map != null && num2 >= num) continue;
                    map = maps[i];
                    num = num2;
                }

            __result = map;
            return false;
        }
    }
}