﻿using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class ReachabilityCache_Patch
    {
        [ThreadStatic] public static List<ReachabilityCache.CachedEntry> tmpCachedEntries;

        public static Dictionary<ReachabilityCache, Dictionary<ReachabilityCache.CachedEntry, bool>> cacheDictDict =
            new Dictionary<ReachabilityCache, Dictionary<ReachabilityCache.CachedEntry, bool>>();

        public static void RunDestructivePatches()
        {
            var original = typeof(ReachabilityCache);
            var patched = typeof(ReachabilityCache_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(get_Count));
            RimThreadedHarmony.Prefix(original, patched, nameof(CachedResultFor));
            RimThreadedHarmony.Prefix(original, patched, nameof(AddCachedResult));
            RimThreadedHarmony.Prefix(original, patched, nameof(Clear));
            RimThreadedHarmony.Prefix(original, patched, nameof(ClearFor));
            RimThreadedHarmony.Prefix(original, patched, nameof(ClearForHostile));
        }

        internal static void InitializeThreadStatics()
        {
            tmpCachedEntries = new List<ReachabilityCache.CachedEntry>();
        }

        public static bool get_Count(ReachabilityCache __instance, ref int __result)
        {
            __result = getCacheDict(__instance).Count;
            return false;
        }

        public static Dictionary<ReachabilityCache.CachedEntry, bool> getCacheDict(ReachabilityCache __instance)
        {
            Dictionary<ReachabilityCache.CachedEntry, bool> cacheDict;
            lock (cacheDictDict)
            {
                if (!cacheDictDict.TryGetValue(__instance, out cacheDict))
                {
                    cacheDict = new Dictionary<ReachabilityCache.CachedEntry, bool>();
                    cacheDictDict[__instance] = cacheDict;
                }
            }

            return cacheDict;
        }

        public static bool CachedResultFor(ReachabilityCache __instance, ref BoolUnknown __result, District A,
            District B, TraverseParms traverseParams)
        {
            if (A == null || B == null)
                return false;
            var cacheDict = getCacheDict(__instance);
            lock (cacheDict)
            {
                if (cacheDict.TryGetValue(new ReachabilityCache.CachedEntry(A.ID, B.ID, traverseParams), out var value))
                {
                    if (!value)
                    {
                        __result = BoolUnknown.False;
                        return false;
                    }

                    __result = BoolUnknown.True;
                    return false;
                }
            }

            __result = BoolUnknown.Unknown;
            return false;
        }

        public static bool AddCachedResult(ReachabilityCache __instance, District A, District B,
            TraverseParms traverseParams, bool reachable)
        {
            if (A == null || B == null)
                return false;
            var key = new ReachabilityCache.CachedEntry(A.ID, B.ID, traverseParams);
            var cacheDict = getCacheDict(__instance);
            if (!cacheDict.ContainsKey(key))
                lock (cacheDict)
                {
                    if (!cacheDict.ContainsKey(key)) cacheDict.Add(key, reachable);
                }

            return false;
        }

        public static bool Clear(ReachabilityCache __instance)
        {
            var cacheDict = getCacheDict(__instance);
            lock (cacheDict)
            {
                cacheDict.Clear();
            }

            return false;
        }

        public static bool ClearFor(ReachabilityCache __instance, Pawn p)
        {
            tmpCachedEntries.Clear();
            var cacheDict = getCacheDict(__instance);

            lock (cacheDict)
            {
                foreach (var item in cacheDict)
                    if (item.Key.TraverseParms.pawn == p)
                        tmpCachedEntries.Add(item.Key);

                for (var i = 0; i < tmpCachedEntries.Count; i++) cacheDict.Remove(tmpCachedEntries[i]);
            }

            //tmpCachedEntries.Clear();
            return false;
        }

        public static bool ClearForHostile(ReachabilityCache __instance, Thing hostileTo)
        {
            if (tmpCachedEntries == null)
                tmpCachedEntries = new List<ReachabilityCache.CachedEntry>();
            else
                tmpCachedEntries.Clear();
            var cacheDict = getCacheDict(__instance);
            lock (cacheDict)
            {
                foreach (var item in cacheDict)
                {
                    var pawn = item.Key.TraverseParms.pawn;
                    if (pawn != null && pawn.HostileTo(hostileTo)) tmpCachedEntries.Add(item.Key);
                }

                for (var i = 0; i < tmpCachedEntries.Count; i++) cacheDict.Remove(tmpCachedEntries[i]);
            }

            //tmpCachedEntries.Clear();
            return false;
        }
    }
}