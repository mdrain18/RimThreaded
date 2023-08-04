using System;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace RimThreaded.RW_Patches
{
    internal class SustainerManager_Patch
    {
        [ThreadStatic] public static Dictionary<SoundDef, List<Sustainer>> playingPerDef;

        internal static void InitializeThreadStatics()
        {
            playingPerDef = new Dictionary<SoundDef, List<Sustainer>>();
        }

        internal static void RunDestructivePatches()
        {
            var original = typeof(SustainerManager);
            var patched = typeof(SustainerManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(RegisterSustainer));
            RimThreadedHarmony.Prefix(original, patched, nameof(DeregisterSustainer));
            //RimThreadedHarmony.Prefix(original, patched, nameof(UpdateAllSustainerScopes));
        }

        public static bool RegisterSustainer(SustainerManager __instance, Sustainer newSustainer)
        {
            lock (__instance.allSustainers)
            {
                __instance.allSustainers.Add(newSustainer);
            }

            return false;
        }

        public static bool DeregisterSustainer(SustainerManager __instance, Sustainer oldSustainer)
        {
            lock (__instance.allSustainers)
            {
                var newSustainers = new List<Sustainer>(__instance.allSustainers);
                newSustainers.Remove(oldSustainer);
                __instance.allSustainers = newSustainers;
            }

            return false;
        }

        public static bool UpdateAllSustainerScopes(SustainerManager __instance)
        {
            playingPerDef.Clear(); //replaced playingPerDef ThreadStatics
            var snapshotSustainers = __instance.allSustainers;
            for (var index = 0; index < snapshotSustainers.Count; ++index)
            {
                var allSustainer = snapshotSustainers[index];
                if (!playingPerDef.ContainsKey(allSustainer.def))
                {
                    var sustainerList = SimplePool_Patch<List<Sustainer>>.Get();
                    sustainerList.Add(allSustainer);
                    playingPerDef.Add(allSustainer.def, sustainerList);
                }
                else
                {
                    playingPerDef[allSustainer.def].Add(allSustainer);
                }
            }

            foreach (var keyValuePair in playingPerDef)
            {
                var key = keyValuePair.Key;
                var sustainerList = keyValuePair.Value;
                if (sustainerList.Count - key.maxVoices < 0)
                {
                    for (var index = 0; index < sustainerList.Count; ++index)
                        sustainerList[index].scopeFader.inScope = true;
                }
                else
                {
                    for (var index = 0; index < sustainerList.Count; ++index)
                        sustainerList[index].scopeFader.inScope = false;
                    sustainerList.Sort(SustainerManager.SortSustainersByCameraDistanceCached);
                    var num = 0;
                    for (var index = 0; index < sustainerList.Count; ++index)
                    {
                        sustainerList[index].scopeFader.inScope = true;
                        ++num;
                        if (num >= key.maxVoices)
                            break;
                    }

                    for (var index = 0; index < sustainerList.Count; ++index)
                        if (!sustainerList[index].scopeFader.inScope)
                            sustainerList[index].scopeFader.inScopePercent = 0.0f;
                }
            }

            foreach (var keyValuePair in playingPerDef)
            {
                keyValuePair.Value.Clear();
                SimplePool_Patch<List<Sustainer>>.Return(keyValuePair.Value);
            }

            //playingPerDef.Clear();
            return false;
        }
    }
}