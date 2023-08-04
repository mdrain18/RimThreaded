﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    internal class Transform_Patch
    {
        private static readonly MethodInfo TransformGetPosition =
            Method(typeof(Transform), nameof(get_position));

        private static readonly MethodInfo TransformGetPositionPatch =
            Method(typeof(Transform_Patch), nameof(get_position));

        private static readonly MethodInfo TransformSetPosition =
            Method(typeof(Transform), nameof(set_position));

        private static readonly MethodInfo TransformSetPositionPatch =
            Method(typeof(Transform_Patch), nameof(set_position));

        private static readonly MethodInfo TransformSetParent =
            Method(typeof(Transform), nameof(set_parent));

        private static readonly MethodInfo TransformSetLocalPosition =
            Method(typeof(Transform), nameof(set_localPosition));

        private static readonly Func<Transform, Vector3> ActionGet_position =
            (Func<Transform, Vector3>) Delegate.CreateDelegate(
                typeof(Func<Transform, Vector3>),
                TransformGetPosition);

        private static readonly Func<object[], object> ActionGet_position2 = parameters =>
            ActionGet_position((Transform) parameters[0]);

        private static readonly Action<Transform, Vector3> ActionSet_position =
            (Action<Transform, Vector3>) Delegate.CreateDelegate(
                typeof(Action<Transform, Vector3>),
                TransformSetPosition);

        private static readonly Action<Transform, Transform> ActionSet_parent =
            (Action<Transform, Transform>) Delegate.CreateDelegate(
                typeof(Action<Transform, Transform>),
                TransformSetParent);

        private static readonly Action<Transform, Vector3> ActionSet_localPosition =
            (Action<Transform, Vector3>) Delegate.CreateDelegate(
                typeof(Action<Transform, Vector3>),
                TransformSetLocalPosition);

        private static readonly Action<object[]> ActionSet_position2 = parameters =>
            ActionSet_position((Transform) parameters[0], (Vector3) parameters[1]);

        private static readonly Action<object[]> ActionSet_parent2 = parameters =>
            ActionSet_parent((Transform) parameters[0], (Transform) parameters[1]);

        private static readonly Action<object[]> ActionSet_localPosition2 = parameters =>
            ActionSet_localPosition((Transform) parameters[0], (Vector3) parameters[1]);


        public static void set_localPosition(Transform __instance, Vector3 value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
            {
                __instance.localPosition = value;
                return;
            }

            threadInfo.safeFunctionRequest = new object[] {ActionSet_localPosition2, new object[] {__instance, value}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
        }

        public static void set_parent(Transform __instance, Transform value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
            {
                __instance.parent = value;
                return;
            }

            threadInfo.safeFunctionRequest = new object[] {ActionSet_parent2, new object[] {__instance, value}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
        }

        public static bool set_position(Transform __instance, Vector3 value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] {ActionSet_position2, new object[] {__instance, value}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

        public static bool get_position(Transform __instance, ref Vector3 __result)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] {ActionGet_position2, new object[] {__instance}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (Vector3) threadInfo.safeFunctionResult;
            return false;
        }

        public static Vector3 get_position2(Transform __instance)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return __instance.position;
            threadInfo.safeFunctionRequest = new object[] {ActionGet_position2, new object[] {__instance}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (Vector3) threadInfo.safeFunctionResult;
        }

        public static void set_position2(Transform __instance, Vector3 value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
            {
                __instance.position = value;
                return;
            }

            threadInfo.safeFunctionRequest = new object[] {ActionSet_position2, new object[] {__instance, value}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
        }

        internal static void RunDestructivePatches()
        {
            var original = typeof(Transform);
            var patched = typeof(Transform_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(get_position));
            RimThreadedHarmony.Prefix(original, patched, nameof(set_position));
            //RimThreadedHarmony.Prefix(original, patched, nameof(set_parent));
        }

        public static IEnumerable<CodeInstruction> TranspileTransformPosition(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            foreach (var codeInstruction in instructions)
            {
                if (codeInstruction.operand is MethodInfo methodInfo)
                {
                    if (methodInfo == TransformGetPosition)
                        codeInstruction.operand = TransformGetPositionPatch;
                    else if (methodInfo == TransformSetPosition) codeInstruction.operand = TransformSetPositionPatch;
                }

                yield return codeInstruction;
            }
        }
    }
}