using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class MapPawns_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(MapPawns);
            var patched = typeof(MapPawns_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(EnsureFactionsListsInit));
            RimThreadedHarmony.Prefix(original, patched, nameof(PawnsInFaction));
            RimThreadedHarmony.Prefix(original, patched, nameof(FreeHumanlikesOfFaction));
            RimThreadedHarmony.Prefix(original, patched, nameof(FreeHumanlikesSpawnedOfFaction));
            RimThreadedHarmony.Prefix(original, patched, nameof(RegisterPawn));
            RimThreadedHarmony.Prefix(original, patched, nameof(DeRegisterPawn));
        }


        public static bool EnsureFactionsListsInit(MapPawns __instance)
        {
            var allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
            var pawnsInFactionSpawned = __instance.pawnsInFactionSpawned;
            var pawnList = pawnsInFactionSpawned.pawnList;
            for (var i = 0; i < allFactionsListForReading.Count; i++)
            {
                var faction = allFactionsListForReading[i];
                if (pawnList.ContainsKey(faction)) continue;
                lock (__instance)
                {
                    if (!pawnList.ContainsKey(faction)) pawnList.Add(faction, new List<Pawn>(32));
                }
            }

            return false;
        }


        public static bool PawnsInFaction(MapPawns __instance, ref List<Pawn> __result, Faction faction)
        {
            if (faction == null)
            {
                Log.Error("Called PawnsInFaction with null faction.");
                __result = new List<Pawn>();
                return false;
            }

            var value = new List<Pawn>(32);
            var allPawns = __instance.AllPawns;
            for (var i = 0; i < allPawns.Count; i++)
            {
                var pawn = allPawns[i];
                if (pawn.Faction == faction) value.Add(pawn);
            }

            lock (__instance)
            {
                __instance.pawnsInFactionResult.pawnList[faction] = value;
            }

            __result = value;
            return false;
        }

        public static bool FreeHumanlikesOfFaction(MapPawns __instance, ref List<Pawn> __result, Faction faction)
        {
            if (faction == null)
            {
                Log.Error("Called FreeHumanlikesOfFaction with null faction.");
                __result = new List<Pawn>();
                return false;
            }

            var value = new List<Pawn>();
            var allPawns = __instance.AllPawns;
            for (var i = 0; i < allPawns.Count; i++)
            {
                var pawn = allPawns[i];
                if (pawn.Faction == faction && pawn.HostFaction == null && pawn.RaceProps.Humanlike) value.Add(pawn);
            }

            lock (__instance)
            {
                __instance.freeHumanlikesSpawnedOfFactionResult.pawnList[faction] = value;
            }

            __result = value;
            return false;
        }

        public static bool FreeHumanlikesSpawnedOfFaction(MapPawns __instance, ref List<Pawn> __result, Faction faction)
        {
            if (faction == null)
            {
                Log.Error("Called FreeHumanlikesSpawnedOfFaction with null faction.");
                __result = new List<Pawn>();
                return false;
            }

            var value = new List<Pawn>();
            var pawnList = __instance.SpawnedPawnsInFaction(faction);
            for (var i = 0; i < pawnList.Count; i++)
            {
                var pawn = pawnList[i];
                if (pawn.HostFaction == null && pawn.RaceProps.Humanlike) value.Add(pawn);
            }

            lock (__instance)
            {
                __instance.freeHumanlikesSpawnedOfFactionResult.pawnList[faction] = value;
            }

            __result = value;
            return false;
        }

        public static bool RegisterPawn(MapPawns __instance, Pawn p)
        {
            if (p.Dead)
            {
                Log.Warning(string.Concat("Tried to register dead pawn ", p, " in ", __instance.GetType(), "."));
                return false;
            }

            if (!p.Spawned)
            {
                Log.Warning(string.Concat("Tried to register despawned pawn ", p, " in ", __instance.GetType(), "."));
                return false;
            }

            if (p.Map != __instance.map)
            {
                Log.Warning(string.Concat("Tried to register pawn ", p, " but his Map is not this one."));
                return false;
            }

            if (!p.mindState.Active) return false;

            EnsureFactionsListsInit(__instance);
            var pawnList = __instance.pawnsSpawned;
            if (!pawnList.Contains(p))
                lock (__instance)
                {
                    if (!pawnList.Contains(p)) pawnList.Add(p);
                }

            var pawnsInFactionSpawned = __instance.pawnsInFactionSpawned.pawnList;
            if (p.Faction != null)
            {
                var pawnList2 = pawnsInFactionSpawned[p.Faction];
                if (!pawnList2.Contains(p))
                    lock (__instance)
                    {
                        if (!pawnList2.Contains(p))
                        {
                            pawnList2.Add(p);
                            if (p.Faction == Faction.OfPlayer)
                                pawnList2.InsertionSort(delegate(Pawn a, Pawn b)
                                {
                                    var num = a.playerSettings?.joinTick ?? 0;
                                    var value = b.playerSettings?.joinTick ?? 0;
                                    return num.CompareTo(value);
                                });
                        }
                    }
            }

            if (p.IsPrisonerOfColony)
            {
                var pawnList3 = __instance.prisonersOfColonySpawned;
                if (!pawnList3.Contains(p))
                    lock (__instance)
                    {
                        if (!pawnList3.Contains(p)) pawnList3.Add(p);
                    }
            }

            __instance.DoListChangedNotifications();

            return false;
        }

        public static bool DeRegisterPawn(MapPawns __instance, Pawn p)
        {
            EnsureFactionsListsInit(__instance);
            lock (__instance)
            {
                var newPawnsSpawned = new List<Pawn>(__instance.pawnsSpawned);
                newPawnsSpawned.Remove(p);
                __instance.pawnsSpawned = newPawnsSpawned;

                var allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
                var newPawnsInFactionSpawned =
                    new Dictionary<Faction, List<Pawn>>(__instance.pawnsInFactionSpawned.pawnList);
                for (var i = 0; i < allFactionsListForReading.Count; i++)
                {
                    var key = allFactionsListForReading[i];
                    newPawnsInFactionSpawned[key].Remove(p);
                }

                __instance.pawnsInFactionSpawned.pawnList = newPawnsInFactionSpawned;
                var newPrisonersOfColonySpawned = new List<Pawn>(__instance.prisonersOfColonySpawned);
                newPrisonersOfColonySpawned.Remove(p);
                __instance.prisonersOfColonySpawned = newPrisonersOfColonySpawned;
            }

            __instance.DoListChangedNotifications();

            return false;
        }
    }
}