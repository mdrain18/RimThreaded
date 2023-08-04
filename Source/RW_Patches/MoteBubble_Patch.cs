﻿using System;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;

namespace RimThreaded.RW_Patches
{
    internal class MoteBubble_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(MoteBubble);
            var patched = typeof(MoteBubble_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(SetupMoteBubble));
        }

        public static bool SetupMoteBubble(MoteBubble __instance, Texture2D icon, Pawn target, Color? iconColor = null)
        {
            __instance.iconMat = MaterialPool.MatFrom(icon, ShaderDatabase.TransparentPostLight, Color.white);
            //__instance.iconMatPropertyBlock = new MaterialPropertyBlock();
            if (!allWorkerThreads.TryGetValue(Thread.CurrentThread, out var threadInfo))
            {
                __instance.iconMatPropertyBlock = new MaterialPropertyBlock();
            }
            else
            {
                Func<object[], object> FuncMaterialPropertyBlock = parameters => new MaterialPropertyBlock();
                threadInfo.safeFunctionRequest = new object[] {FuncMaterialPropertyBlock, new object[] { }};
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __instance.iconMatPropertyBlock = (MaterialPropertyBlock) threadInfo.safeFunctionResult;
            }

            __instance.arrowTarget = target;
            if (!iconColor.HasValue)
                return false;
            __instance.iconMatPropertyBlock.SetColor("_Color", iconColor.Value);
            return false;
        }
    }
}