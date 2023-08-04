using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Verse;

namespace RimThreaded
{
#pragma warning disable 649
    [Serializable]
    public class SerializableMethod
    {
        public string Name;
        public Type ClassType;
        public List<Type> ParametersType;

        public SerializableMethod()
        {
            ParametersType = new List<Type>();
        }
    }
#pragma warning restore 649
    public class AssemblyCache
    {
        private static readonly List<MethodBase> CacheList = new List<MethodBase>();
        private static List<SerializableMethod> CacheListS = new List<SerializableMethod>();
        private static string CurrentMethodPath;

        public static void SaveJson()
        {
            var jsonString = JsonConvert.SerializeObject(CacheListS, Formatting.Indented);
            File.WriteAllText(CurrentMethodPath, jsonString);
            CacheList.Clear();
            CacheListS.Clear();
        }

        public static bool TryGetFromCache(string ModuleVersionId, out List<MethodBase> ReturnMethodList)
        {
            if (Prefs.LogVerbose) Log.Message("TryGetFromCache: " + ModuleVersionId);
            var CacheFolder = Path.Combine(RimThreadedMod.replacementsFolder, "Caches");
            Directory.CreateDirectory(CacheFolder);
            CurrentMethodPath = Path.Combine(CacheFolder, ModuleVersionId + ".json");
            if (!File.Exists(CurrentMethodPath))

            {
                ReturnMethodList = null;
                return false;
            }

            if (Prefs.LogVerbose)
                Log.Message("RimThreaded is loading Cached Field Replacements from: " + CurrentMethodPath);
            var jsonstr = File.ReadAllText(CurrentMethodPath);
            CacheListS = JsonConvert.DeserializeObject<List<SerializableMethod>>(jsonstr);
            foreach (var s in CacheListS)
            {
                if (s.Name == ".ctor")
                {
                    CacheList.Add(AccessTools.Constructor(s.ClassType, s.ParametersType.ToArray()));
                    continue;
                }

                if (s.Name == ".cctor")
                {
                    CacheList.Add(AccessTools.Constructor(s.ClassType, s.ParametersType.ToArray(), true));
                    continue;
                }

                CacheList.Add(AccessTools.Method(s.ClassType, s.Name, s.ParametersType.ToArray()));
            }

            ReturnMethodList = CacheList;
            return true;
        }

        public static void AddToCache(string AssemblyName, MethodBase method, Type type)
        {
            var CacheFolder = Path.Combine(RimThreadedMod.replacementsFolder, "Caches");
            var SMethod = new SerializableMethod
            {
                Name = method.Name,
                ClassType = type
            };
            foreach (var p in method.GetParameters()) SMethod.ParametersType.Add(p.ParameterType);
            CacheListS.Add(SMethod);
            CurrentMethodPath = Path.Combine(CacheFolder, AssemblyName + ".json");
            CacheList.Add(method);
        }
    }
}