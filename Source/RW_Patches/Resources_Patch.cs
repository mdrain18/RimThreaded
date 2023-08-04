using System;
using UnityEngine;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;
using Object = UnityEngine.Object;

namespace RimThreaded.RW_Patches
{
    internal class Resources_Patch
    {
        public static System.Func<object[], Object> safeFunction = parameters =>
            Resources.Load(
                (string) parameters[0],
                (Type) parameters[1]);

        public static Object Load(string path, Type type)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return Resources.Load(path, type);
            threadInfo.safeFunctionRequest = new object[] {safeFunction, new object[] {path, type}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (Object) threadInfo.safeFunctionResult;
        }
    }
}