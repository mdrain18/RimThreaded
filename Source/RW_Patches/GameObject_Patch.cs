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
    internal class GameObject_Patch
    {
        private static readonly MethodInfo methodInternal_CreateGameObject =
            Method(typeof(GameObject), "Internal_CreateGameObject", new[]
                {typeof(GameObject), typeof(string)});

        private static readonly Action<GameObject, string> ActionInternal_CreateGameObject =
            (Action<GameObject, string>) Delegate.CreateDelegate(
                typeof(Action<GameObject, string>), methodInternal_CreateGameObject);

        private static readonly Action<object[]> ActionGameObject = parameters =>
            ActionInternal_CreateGameObject((GameObject) parameters[0], (string) parameters[1]);

        private static readonly MethodInfo methodInternal_CreateGameObject_Patch =
            Method(typeof(GameObject_Patch), "Internal_CreateGameObject", new[]
                {typeof(GameObject), typeof(string)});

        private static readonly MethodInfo MethodGameObjectTransform = Method(typeof(GameObject), "get_transform");

        private static readonly MethodInfo MethodGameObject_PatchTransform =
            Method(typeof(GameObject_Patch), "get_transform");


        public static void Internal_CreateGameObject(GameObject gameObject, string name)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
            {
                ActionInternal_CreateGameObject(gameObject, name);
                return;
            }

            threadInfo.safeFunctionRequest = new object[] {ActionGameObject, new object[] {gameObject, name}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
        }

        public static void RunNonDestructivePatches()
        {
            var original = typeof(GameObject);
            var patched = typeof(GameObject_Patch);

            var transpilerMethod = new HarmonyMethod(Method(patched, "TranspileGameObjectString"));
            RimThreadedHarmony.harmony.Patch(Constructor(original,
                new[] {typeof(string)}), transpiler: transpilerMethod);
            RimThreadedHarmony.harmony.Patch(Constructor(original,
                new[] {typeof(string), typeof(Type[])}), transpiler: transpilerMethod);
            RimThreadedHarmony.harmony.Patch(Constructor(original,
                Type.EmptyTypes), transpiler: transpilerMethod);
        }

        public static IEnumerable<CodeInstruction> TranspileGameObjectString(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            foreach (var codeInstruction in instructions)
            {
                if (codeInstruction.operand is MethodInfo methodInfo)
                    if (methodInfo == methodInternal_CreateGameObject)
                        codeInstruction.operand = methodInternal_CreateGameObject_Patch;
                yield return codeInstruction;
            }
        }

        public static T GetComponent<T>(GameObject __instance)
        {
            if (!allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo)) return __instance.GetComponent<T>();
            Func<object[], object> FuncGetComponent = parameters => __instance.GetComponent<T>();
            threadInfo.safeFunctionRequest = new object[] {FuncGetComponent, new object[] { }};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (T) threadInfo.safeFunctionResult;
        }

        public static T AddComponent<T>(GameObject __instance) where T : Component
        {
            if (!allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo)) return __instance.AddComponent<T>();
            Func<object[], object> FuncAddComponent = parameters => __instance.AddComponent<T>();
            threadInfo.safeFunctionRequest = new object[] {FuncAddComponent, new object[] { }};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (T) threadInfo.safeFunctionResult;
        }

        public static AudioReverbFilter GetComponentAudioReverbFilter<AudioReverbFilter>(GameObject __instance)
        {
            if (!allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return __instance.GetComponent<AudioReverbFilter>();
            Func<object[], object> FuncGetComponent = parameters => __instance.GetComponent<AudioReverbFilter>();
            threadInfo.safeFunctionRequest = new object[] {FuncGetComponent, new object[] { }};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (AudioReverbFilter) threadInfo.safeFunctionResult;
        }

        public static AudioLowPassFilter GetComponentAudioLowPassFilter<AudioLowPassFilter>(GameObject __instance)
        {
            if (!allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return __instance.GetComponent<AudioLowPassFilter>();
            Func<object[], object> FuncGetComponent = parameters => __instance.GetComponent<AudioLowPassFilter>();
            threadInfo.safeFunctionRequest = new object[] {FuncGetComponent, new object[] { }};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (AudioLowPassFilter) threadInfo.safeFunctionResult;
        }

        public static AudioHighPassFilter GetComponentAudioHighPassFilter<AudioHighPassFilter>(GameObject __instance)
        {
            if (!allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return __instance.GetComponent<AudioHighPassFilter>();
            Func<object[], object> FuncGetComponent = parameters => __instance.GetComponent<AudioHighPassFilter>();
            threadInfo.safeFunctionRequest = new object[] {FuncGetComponent, new object[] { }};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (AudioHighPassFilter) threadInfo.safeFunctionResult;
        }

        public static AudioEchoFilter GetComponentAudioEchoFilter<AudioEchoFilter>(GameObject __instance)
        {
            if (!allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return __instance.GetComponent<AudioEchoFilter>();
            Func<object[], object> FuncGetComponent = parameters => __instance.GetComponent<AudioEchoFilter>();
            threadInfo.safeFunctionRequest = new object[] {FuncGetComponent, new object[] { }};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (AudioEchoFilter) threadInfo.safeFunctionResult;
        }


        public static Transform get_transform(GameObject __instance)
        {
            if (!allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo)) return __instance.transform;
            Func<object[], object> FuncTransform = parameters => __instance.transform;
            threadInfo.safeFunctionRequest = new object[] {FuncTransform, new object[] { }};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (Transform) threadInfo.safeFunctionResult;
        }


        public static IEnumerable<CodeInstruction> TranspileGameObjectTransform(
            IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (var codeInstruction in instructions)
            {
                if (codeInstruction.operand is MethodInfo methodInfo)
                    if (methodInfo == MethodGameObjectTransform)
                        //Log.Message("RimThreaded is replacing method call: ");
                        codeInstruction.operand = MethodGameObject_PatchTransform;
                yield return codeInstruction;
            }
        }
    }
}