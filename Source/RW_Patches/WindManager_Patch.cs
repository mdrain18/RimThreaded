using System;
using System.Threading;
using UnityEngine;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class WindManager_Patch
    {
        //public static List<Material> plantMaterialsList;
        public static int plantMaterialsCount;
        public static float plantSwayHead;

        internal static void RunDestructivePatches()
        {
            var original = typeof(WindManager);
            var patched = typeof(WindManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "WindManagerTick");
        }

        public static bool WindManagerTick(WindManager __instance)
        {
            __instance.cachedWindSpeed = __instance.BaseWindSpeedAt(Find.TickManager.TicksAbs) *
                                         __instance.map.weatherManager.CurWindSpeedFactor;
            var curWindSpeedOffset = __instance.map.weatherManager.CurWindSpeedOffset;
            if (curWindSpeedOffset > 0f)
            {
                var floatRange = WindManager.WindSpeedRange * __instance.map.weatherManager.CurWindSpeedFactor;
                var num = (__instance.cachedWindSpeed - floatRange.min) / (floatRange.max - floatRange.min) *
                          (floatRange.max - curWindSpeedOffset);
                __instance.cachedWindSpeed = curWindSpeedOffset + num;
            }

            var list = __instance.map.listerThings.ThingsInGroup(ThingRequestGroup.WindSource);
            for (var i = 0; i < list.Count; i++)
            {
                var compWindSource = list[i].TryGetComp<CompWindSource>();
                __instance.cachedWindSpeed = Mathf.Max(__instance.cachedWindSpeed, compWindSource.wind);
            }

            if (Prefs.PlantWindSway)
                __instance.plantSwayHead += Mathf.Min(__instance.WindSpeed, 1f);
            else
                __instance.plantSwayHead = 0f;

            if (Find.CurrentMap != __instance.map) return false;
            plantSwayHead = __instance.plantSwayHead;
            plantMaterialsCount = WindManager.plantMaterials.Count;
            return false;
        }

        public static void WindManagerPrepare()
        {
            var maps = Find.Maps;
            for (var i = 0; i < maps.Count; i++) maps[i].MapPreTick();
        }

        public static void WindManagerListTick()
        {
            while (true)
            {
                var index = Interlocked.Decrement(ref plantMaterialsCount);
                if (index < 0) return;
                var material = WindManager.plantMaterials[index];
                try
                {
                    material.SetFloat(ShaderPropertyIDs.SwayHead, plantSwayHead);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception ticking " + WindManager.plantMaterials[index].ToStringSafe() + ": " + ex);
                }
            }
        }
    }
}