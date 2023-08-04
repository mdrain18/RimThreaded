﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class TaleManager_Patch
    {
        public static void RunDestructivePatches()
        {
            var original = typeof(TaleManager);
            var patched = typeof(TaleManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Add");
            RimThreadedHarmony.Prefix(original, patched, "RemoveTale");
        }

        public static bool Add(TaleManager __instance, Tale tale)
        {
            lock (__instance)
            {
                __instance.tales.Add(tale);
            }

            __instance.CheckCullTales(tale);
            return false;
        }

        public static bool RemoveTale(TaleManager __instance, Tale tale)
        {
            if (!tale.Unused)
                Log.Warning("Tried to remove used tale " + tale);
            else
                lock (__instance)
                {
                    var newTales = new List<Tale>(__instance.tales);
                    newTales.Remove(tale);
                    __instance.tales = newTales;
                }

            return false;
        }
    }
}