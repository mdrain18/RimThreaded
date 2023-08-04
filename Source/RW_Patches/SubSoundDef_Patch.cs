﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;

namespace RimThreaded.RW_Patches
{
    public class SubSoundDef_Patch
    {
        //1.4 TODO better initialize?
        public static Dictionary<SubSoundDef, ConcurrentQueue<ResolvedGrain>> recentlyPlayedResolvedGrainsDictionary =
            new Dictionary<SubSoundDef, ConcurrentQueue<ResolvedGrain>>();

        public static void RunDestructivePatches()
        {
            var original = typeof(SubSoundDef);
            var patched = typeof(SubSoundDef_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_GrainPlayed));
            RimThreadedHarmony.Prefix(original, patched, nameof(RandomizedResolvedGrain));
        }

        public static bool Notify_GrainPlayed(SubSoundDef __instance, ResolvedGrain chosenGrain)
        {
            var distinctResolvedGrainsCount = __instance.distinctResolvedGrainsCount;
            if (distinctResolvedGrainsCount <= 1) return false;
            var repeatMode = __instance.repeatMode;

            if (!recentlyPlayedResolvedGrainsDictionary.TryGetValue(__instance, out var recentlyPlayedResolvedGrains))
                lock (recentlyPlayedResolvedGrainsDictionary)
                {
                    if (!recentlyPlayedResolvedGrainsDictionary.TryGetValue(__instance,
                            out recentlyPlayedResolvedGrains))
                    {
                        recentlyPlayedResolvedGrains = new ConcurrentQueue<ResolvedGrain>();
                        recentlyPlayedResolvedGrainsDictionary[__instance] = recentlyPlayedResolvedGrains;
                    }
                }

            if (repeatMode == RepeatSelectMode.NeverLastHalf)
            {
                var numToAvoid = __instance.numToAvoid;
                while (recentlyPlayedResolvedGrains.Count >= numToAvoid) recentlyPlayedResolvedGrains.TryDequeue(out _);
                if (recentlyPlayedResolvedGrains.Count < numToAvoid) recentlyPlayedResolvedGrains.Enqueue(chosenGrain);
            }
            else if (repeatMode == RepeatSelectMode.NeverTwice)
            {
                __instance.lastPlayedResolvedGrain = chosenGrain;
            }

            return false;
        }

        public static bool RandomizedResolvedGrain(SubSoundDef __instance, ref ResolvedGrain __result)
        {
            ResolvedGrain chosenGrain = null;
            var resolvedGrains = __instance.resolvedGrains;
            var distinctResolvedGrainsCount = __instance.distinctResolvedGrainsCount;
            var repeatMode = __instance.repeatMode;
            if (!recentlyPlayedResolvedGrainsDictionary.TryGetValue(__instance, out var recentlyPlayedResolvedGrains))
                lock (recentlyPlayedResolvedGrainsDictionary)
                {
                    if (!recentlyPlayedResolvedGrainsDictionary.TryGetValue(__instance,
                            out recentlyPlayedResolvedGrains))
                    {
                        recentlyPlayedResolvedGrains = new ConcurrentQueue<ResolvedGrain>();
                        recentlyPlayedResolvedGrainsDictionary[__instance] = recentlyPlayedResolvedGrains;
                    }
                }

            while (true)
            {
                chosenGrain = resolvedGrains.RandomElement();
                if (distinctResolvedGrainsCount <= 1) break;
                if (repeatMode == RepeatSelectMode.NeverLastHalf)
                {
                    if (!recentlyPlayedResolvedGrains.Where(g => g.Equals(chosenGrain)).Any()) break;
                }
                else if (repeatMode != RepeatSelectMode.NeverTwice ||
                         !chosenGrain.Equals(__instance.lastPlayedResolvedGrain))
                {
                    break;
                }
            }

            __result = chosenGrain;
            return false;
        }
    }
}