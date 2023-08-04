using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    public class MapGenerator_Patch
    {
        private static readonly Func<object[], object> FuncGenerateMap = parameters =>
            MapGenerator.GenerateMap(
                (IntVec3) parameters[0],
                (MapParent) parameters[1],
                (MapGeneratorDef) parameters[2],
                (IEnumerable<GenStepWithParams>) parameters[3],
                (Action<Map>) parameters[4]);


        internal static void RunDestructivePatches()
        {
            var original = typeof(MapGenerator);
            var patched = typeof(MapGenerator_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GenerateMap");
        }

        public static bool GenerateMap(ref Map __result, IntVec3 mapSize, MapParent parent,
            MapGeneratorDef mapGenerator, IEnumerable<GenStepWithParams> extraGenStepDefs = null,
            Action<Map> extraInitBeforeContentGen = null)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.timeoutExempt = 60000;
            threadInfo.safeFunctionRequest = new object[]
            {
                FuncGenerateMap, new object[]
                {
                    mapSize, parent, mapGenerator, extraGenStepDefs, extraInitBeforeContentGen
                }
            };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            threadInfo.timeoutExempt = 0;
            __result = (Map) threadInfo.safeFunctionResult;
            return false;
        }
    }
}