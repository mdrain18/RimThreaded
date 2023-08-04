using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class ImmunityHandler_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(ImmunityHandler);
            var patched = typeof(ImmunityHandler_Patch);
            RimThreadedHarmony.Prefix(original, patched, "ImmunityHandlerTick");
        }

        public static bool ImmunityHandlerTick(ImmunityHandler __instance)
        {
            var list = __instance.NeededImmunitiesNow();
            for (var i = 0; i < list.Count; i++) __instance.TryAddImmunityRecord(list[i].immunity, list[i].source);
            lock (__instance)
            {
                var newImmunityList = new List<ImmunityRecord>(__instance.immunityList);
                for (var j = 0; j < __instance.immunityList.Count; j++)
                {
                    var immunityRecord = newImmunityList[j];
                    var firstHediffOfDef =
                        __instance.pawn.health.hediffSet.GetFirstHediffOfDef(immunityRecord.hediffDef);
                    immunityRecord.ImmunityTick(__instance.pawn, firstHediffOfDef != null, firstHediffOfDef);
                }

                for (var num = newImmunityList.Count - 1; num >= 0; num--)
                    if (newImmunityList[num].immunity <= 0f)
                    {
                        var flag = false;
                        for (var k = 0; k < list.Count; k++)
                            if (list[k].immunity == newImmunityList[num].hediffDef)
                            {
                                flag = true;
                                break;
                            }

                        if (!flag) newImmunityList.RemoveAt(num);
                    }

                __instance.immunityList = newImmunityList;
            }

            return false;
        }
    }
}