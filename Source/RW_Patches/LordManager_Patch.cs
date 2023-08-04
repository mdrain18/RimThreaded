using Verse;
using Verse.AI.Group;

namespace RimThreaded.RW_Patches
{
    internal class LordManager_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(LordManager);
            var patched = typeof(LordManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "LordOf", new[] {typeof(Pawn)});
            RimThreadedHarmony.Prefix(original, patched, "RemoveLord");
        }

        public static bool LordOf(LordManager __instance, ref Lord __result, Pawn p)
        {
            Lord lordResult = null;
            if (p != null)
                if (!Lord_Patch.pawnsLord.TryGetValue(p, out lordResult))
                {
                    for (var i = 0; i < __instance.lords.Count; i++)
                    {
                        var lord = __instance.lords[i];
                        for (var j = 0; j < lord.ownedPawns.Count; j++)
                            if (lord.ownedPawns[j] == p)
                            {
                                lock (Lord_Patch.pawnsLord)
                                {
                                    Lord_Patch.pawnsLord.SetOrAdd(p, lord);
                                }

                                __result = lord;
                                return false;
                            }
                    }

                    lock (Lord_Patch.pawnsLord)
                    {
                        Lord_Patch.pawnsLord.SetOrAdd(p, null);
                    }
                }

            __result = lordResult;
            return false;
        }

        public static bool RemoveLord(LordManager __instance, Lord oldLord)
        {
            for (var j = 0; j < oldLord.ownedPawns.Count; j++)
                lock (Lord_Patch.pawnsLord)
                {
                    Lord_Patch.pawnsLord.SetOrAdd(oldLord.ownedPawns[j], null);
                }

            __instance.lords.Remove(oldLord);
            Find.SignalManager.DeregisterReceiver(oldLord);
            oldLord.Cleanup();
            return false;
        }
    }
}