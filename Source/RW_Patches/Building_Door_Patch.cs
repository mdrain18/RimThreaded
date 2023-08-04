using System;
using RimWorld;

namespace RimThreaded.RW_Patches
{
    public class Building_Door_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(Building_Door);
            var patched = typeof(Building_Door_Patch);
            RimThreadedHarmony.Prefix(original, patched, "get_DoorPowerOn");
        }

        public static bool get_DoorPowerOn(Building_Door __instance, ref bool __result)
        {
            var pc = __instance.powerComp;
            var poweron = false;
            if (pc != null)
                try
                {
                    poweron = pc.PowerOn;
                }
                catch (NullReferenceException)
                {
                }

            __result = poweron;
            return false;
        }
    }
}