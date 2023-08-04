using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class StatWorker_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(StatWorker);
            var patched = typeof(StatWorker_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(GetValue),
                new[] {typeof(Thing), typeof(bool), typeof(int)});
        }

        public static bool GetValue(StatWorker __instance, ref float __result, Thing thing,
            bool applyPostProcess = true, int cacheStaleAfterTicks = -1)
        {
            var stat = __instance.stat;
            var immutableStatCache = __instance.immutableStatCache;
            if (stat.immutable)
            {
                if (immutableStatCache.TryGetValue(thing, out __result)) return false;
                lock (immutableStatCache)
                {
                    if (immutableStatCache.TryGetValue(thing, out __result)) return false;
                    __result = __instance.GetValue(StatRequest.For(thing));
                    immutableStatCache[thing] = __result;
                    return false;
                }
            }

            var temporaryStatCache = __instance.temporaryStatCache;
            var ticksGame = Find.TickManager.TicksGame;
            if (temporaryStatCache == null)
            {
                __result = __instance.GetValue(StatRequest.For(thing));
                return false;
            }

            if (cacheStaleAfterTicks != -1 && temporaryStatCache.TryGetValue(thing, out var statCacheEntry) &&
                ticksGame - statCacheEntry.gameTick < cacheStaleAfterTicks)
            {
                __result = statCacheEntry.statValue;
                return false;
            }

            __result = __instance.GetValue(StatRequest.For(thing));
            lock (temporaryStatCache)
            {
                if (temporaryStatCache.TryGetValue(thing, out var statCacheEntry2))
                {
                    statCacheEntry2.statValue = __result;
                    statCacheEntry2.gameTick = ticksGame;
                    return false;
                }

                temporaryStatCache[thing] = new StatCacheEntry(__result, ticksGame);
                return false;
            }
        }
    }
}