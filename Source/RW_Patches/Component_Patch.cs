﻿using System;
using UnityEngine;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    internal class Component_Patch
    {
        //private static readonly MethodInfo MethodComponentTransform = Method(typeof(Component), "get_transform");
        //private static readonly MethodInfo MethodComponent_PatchTransform = Method(typeof(Component_Patch), "get_transform");

        public static Transform get_transform(Component __instance)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return __instance.transform;
            Func<object[], object> FuncTransform = parameters => __instance.transform;
            threadInfo.safeFunctionRequest = new object[] {FuncTransform, new object[] { }};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (Transform) threadInfo.safeFunctionResult;
        }

        public static GameObject get_gameObject(Component __instance)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return __instance.gameObject;
            Func<object[], object> FuncGameObject = parameters => __instance.gameObject;
            threadInfo.safeFunctionRequest = new object[] {FuncGameObject, new object[] { }};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (GameObject) threadInfo.safeFunctionResult;
        }

        //public static IEnumerable<CodeInstruction> TranspileComponentTransform(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        //{
        //    foreach (CodeInstruction codeInstruction in instructions)
        //    {
        //        if (codeInstruction.operand is MethodInfo methodInfo)
        //        {
        //            if (methodInfo == MethodComponentTransform)
        //            {
        //                //Log.Message("RimThreaded is replacing method call: ");
        //                codeInstruction.operand = MethodComponent_PatchTransform;
        //            }
        //        }
        //        yield return codeInstruction;
        //    }
        //}
    }
}