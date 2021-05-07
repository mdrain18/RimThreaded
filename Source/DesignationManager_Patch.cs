﻿using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class DesignationManager_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(DesignationManager);
            Type patched = typeof(DesignationManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RemoveDesignation");
            RimThreadedHarmony.Prefix(original, patched, "RemoveAllDesignationsOn");
            RimThreadedHarmony.Prefix(original, patched, "RemoveAllDesignationsOfDef");
            RimThreadedHarmony.Prefix(original, patched, "AddDesignation");
            RimThreadedHarmony.Prefix(original, patched, "SpawnedDesignationsOfDef");
        }

        private static readonly Action<Designation> ActionNotify_Removing =
            (Action<Designation>)Delegate.CreateDelegate(
                typeof(Action<Designation>),
                Method(typeof(Designation), "Notify_Removing"));


        public static bool RemoveDesignation(DesignationManager __instance, Designation des)
        {
            ActionNotify_Removing(des);
            if (!__instance.allDesignations.Contains(des)) return false;

            lock (__instance)
            {
                List<Designation> newAllDesignations = new List<Designation>(__instance.allDesignations);
                newAllDesignations.Remove(des);
                __instance.allDesignations = newAllDesignations;
            }
            return false;
        }
        public static bool RemoveAllDesignationsOn(DesignationManager __instance, Thing t, bool standardCanceling = false)
        {
            bool matchFound = false;
            for (int index = 0; index < __instance.allDesignations.Count; ++index)
            {
                Designation designation = __instance.allDesignations[index];
                if ((!standardCanceling || designation.def.designateCancelable) && designation.target.Thing == t)
                {
                    ActionNotify_Removing(designation);
                    matchFound = true;
                }
            }
            if (!matchFound) return false;
            lock (__instance)
            {
                List<Designation> newAllDesignations = new List<Designation>(__instance.allDesignations);
                newAllDesignations.RemoveAll((Predicate<Designation>)(d => (!standardCanceling || d.def.designateCancelable) && d.target.Thing == t));
                __instance.allDesignations = newAllDesignations;
            }
            
            return false;
        }
        public static bool RemoveAllDesignationsOfDef(DesignationManager __instance, DesignationDef def)
        {
            lock (__instance)
            {
                List<Designation> newAllDesignations = new List<Designation>(__instance.allDesignations);
                for (int index = newAllDesignations.Count - 1; index >= 0; --index)
                {
                    if (newAllDesignations[index].def != def) continue;
                    
                    ActionNotify_Removing(newAllDesignations[index]);
                    newAllDesignations.RemoveAt(index);
                }
                __instance.allDesignations = newAllDesignations;
            }

            return false;
        }
        public static bool AddDesignation(DesignationManager __instance, Designation newDes)
        {
            if (newDes.def.targetType == TargetType.Cell && __instance.DesignationAt(newDes.target.Cell, newDes.def) != null)
                Log.Error("Tried to double-add designation at location " + (object)newDes.target);
            else if (newDes.def.targetType == TargetType.Thing && __instance.DesignationOn(newDes.target.Thing, newDes.def) != null)
            {
                Log.Error("Tried to double-add designation on Thing " + (object)newDes.target);
            }
            else
            {
                if (newDes.def.targetType == TargetType.Thing)
                    newDes.target.Thing.SetForbidden(false, false);
                lock (__instance)
                {
                    __instance.allDesignations.Add(newDes);
                }

                newDes.designationManager = __instance;
                newDes.Notify_Added();
                Map map = newDes.target.HasThing ? newDes.target.Thing.Map : __instance.map;
                if (map == null)
                    return false;
                MoteMaker.ThrowMetaPuffs(newDes.target.ToTargetInfo(map));
            }
            return false;
        }

        public static bool SpawnedDesignationsOfDef(DesignationManager __instance, ref IEnumerable<Designation> __result,
            DesignationDef def)
        {
            __result = SpawnedDesignationsOfDefEnumerable(__instance, def);
            return false;
        }

        public static IEnumerable<Designation> SpawnedDesignationsOfDefEnumerable(DesignationManager __instance,
            DesignationDef def)
        {
            List<Designation> allDesignationsSnapshot = __instance.allDesignations;
            int count = allDesignationsSnapshot.Count;
            for (int i = 0; i < count; ++i)
            {
                Designation allDesignation = allDesignationsSnapshot[i];
                if (allDesignation.def == def && (!allDesignation.target.HasThing || allDesignation.target.Thing.Map == __instance.map))
                    yield return allDesignation;
            }
        }
    }
}
