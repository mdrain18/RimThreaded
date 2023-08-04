﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    internal class CombatExteneded_Patch
    {
        public static Type combatExtendedCE_Utility;
        public static Type combatExtendedVerb_LaunchProjectileCE;
        public static Type combatExtendedVerb_MeleeAttackCE;
        public static Type combatExtended_ProjectileCE;

        public static void Patch()
        {
            combatExtendedCE_Utility = TypeByName("CombatExtended.CE_Utility");
            combatExtendedVerb_LaunchProjectileCE = TypeByName("CombatExtended.Verb_LaunchProjectileCE");
            combatExtendedVerb_MeleeAttackCE = TypeByName("CombatExtended.Verb_MeleeAttackCE");
            combatExtended_ProjectileCE = TypeByName("CombatExtended.ProjectileCE");

            Type patched;
            if (combatExtendedCE_Utility != null)
            {
                var methodName = "BlitCrop";
                Log.Message("RimThreaded is patching " + combatExtendedCE_Utility.FullName + " " + methodName);
                patched = typeof(CE_Utility_Transpile);
                Transpile(combatExtendedCE_Utility, patched, methodName);
                methodName = "GetColorSafe";
                Log.Message("RimThreaded is patching " + combatExtendedCE_Utility.FullName + " " + methodName);
                Transpile(combatExtendedCE_Utility, patched, methodName);
            }


            var CE_ThingsTrackingModel = TypeByName("CombatExtended.Utilities.ThingsTrackingModel");
            if (CE_ThingsTrackingModel != null)
            {
                var methodName = "Register";
                Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(CE_ThingsTrackingModel, methodName, LockFlag.WriterLock);
                methodName = "DeRegister";
                Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(CE_ThingsTrackingModel, methodName, LockFlag.WriterLock);
                methodName = "Notify_ThingPositionChanged";
                Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(CE_ThingsTrackingModel, methodName, LockFlag.WriterLock);
                methodName = "RemoveClean";
                Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(CE_ThingsTrackingModel, methodName, LockFlag.WriterLock);
                /*
                string methodName = "Register";
                Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName);
                RimThreadedHarmony.TranspileMethodLock(CE_ThingsTrackingModel, methodName);
                string methodName2 = "DeRegister";
                Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName2);
                RimThreadedHarmony.TranspileMethodLock(CE_ThingsTrackingModel, methodName2);
                string methodName3 = "Notify_ThingPositionChanged";
                Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName3);
                RimThreadedHarmony.TranspileMethodLock(CE_ThingsTrackingModel, methodName3);//lock should be reentrant otherwise this is an obvious deadlock.
                string methodName4 = "RemoveClean";
                Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName4);
                RimThreadedHarmony.TranspileMethodLock(CE_ThingsTrackingModel, methodName4);*/
            }

            //if (combatExtendedVerb_LaunchProjectileCE != null)
            //{
            //	string methodName = "CanHitFromCellIgnoringRange";
            //	patched = typeof(Verb_LaunchProjectileCE_Transpile);
            //	Log.Message("RimThreaded is patching " + combatExtendedVerb_LaunchProjectileCE.FullName + " " + methodName);
            //	Transpile(combatExtendedVerb_LaunchProjectileCE, patched, methodName);
            //	methodName = "TryFindCEShootLineFromTo";
            //	Log.Message("RimThreaded is patching " + combatExtendedVerb_LaunchProjectileCE.FullName + " " + methodName);
            //	Transpile(combatExtendedVerb_LaunchProjectileCE, patched, methodName);
            //}
            //if (combatExtendedVerb_MeleeAttackCE != null)
            //{
            //	string methodName = "TryCastShot";
            //	patched = typeof(Verb_MeleeAttackCE_Transpile);
            //	Log.Message("RimThreaded is patching " + combatExtendedVerb_MeleeAttackCE.FullName + " " + methodName);
            //	Transpile(combatExtendedVerb_MeleeAttackCE, patched, methodName);
            //}
            if (combatExtendedVerb_MeleeAttackCE != null)
            {
                var methodName = "TryCastShot";
                patched = typeof(CombatExteneded_Patch);
                Log.Message("RimThreaded is patching " + combatExtendedVerb_MeleeAttackCE.FullName + " " + methodName);
                Transpile(combatExtendedVerb_MeleeAttackCE, patched, methodName);
            }
        }


        public static IEnumerable<CodeInstruction> TryCastShot(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var i = 0;
            var applyMeleeDamageToTarget =
                Method(typeof(Verb_MeleeAttack), nameof(Verb_MeleeAttack.ApplyMeleeDamageToTarget));
            while (i < instructionsList.Count)
            {
                var ci = instructionsList[i];
                if (ci.opcode == OpCodes.Callvirt && (MethodInfo) ci.operand == applyMeleeDamageToTarget)
                {
                    yield return ci;
                    //yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6); // load pawn target
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // verb_MeleeAttack (this)
                    yield return new CodeInstruction(OpCodes.Call,
                        Method(typeof(CombatExteneded_Patch), nameof(PreApplyMeleeSlaveSuppression)));
                    i++;
                    //yield return instructionsList[i]; // ldloc.s 20
                    //i++;
                    //yield return instructionsList[i]; // AssociateWithLog
                }
                else
                {
                    yield return instructionsList[i];
                    i++;
                }
            }
        }

        public static DamageWorker.DamageResult PreApplyMeleeSlaveSuppression(DamageWorker.DamageResult damageResult,
            Pawn pawn, Verb_MeleeAttack verb_MeleeAttack)
        {
            if (pawn != null && damageResult.totalDamageDealt > 0f)
                verb_MeleeAttack.ApplyMeleeSlaveSuppression(pawn, damageResult.totalDamageDealt);
            return damageResult;
        }
    }
}