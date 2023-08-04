﻿using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    internal class DrugAIUtility_Patch
    {
        public static void RunDestructivePatches()
        {
            //DrugAIUtility - vanilla bug?
            var original = typeof(DrugAIUtility);
            var patched = typeof(DrugAIUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, "IngestAndTakeToInventoryJob");
        }

        public static bool IngestAndTakeToInventoryJob(ref Job __result, Thing drug, Pawn pawn,
            int maxNumToCarry = 9999)
        {
            var job = JobMaker.MakeJob(JobDefOf.Ingest, drug);
            job.count = Mathf.Min(drug.stackCount, drug.def.ingestible.maxNumToIngestAtOnce, maxNumToCarry);
            if (pawn.drugs != null && drugPolicyExists(pawn.drugs.CurrentPolicy.entriesInt, drug.def))
            {
                var drugPolicyEntry = pawn.drugs.CurrentPolicy[drug.def];
                var num = pawn.inventory.innerContainer.TotalStackCountOfDef(drug.def) - job.count;
                if (drugPolicyEntry.allowScheduled && num <= 0)
                    job.takeExtraIngestibles = drugPolicyEntry.takeToInventory;
            }

            __result = job;
            return false;
        }

        private static bool drugPolicyExists(List<DrugPolicyEntry> entriesInt, ThingDef def)
        {
            for (var index = 0; index < entriesInt.Count; index++)
                if (entriesInt[index].drug == def)
                    return true;
            return false;
        }
    }
}