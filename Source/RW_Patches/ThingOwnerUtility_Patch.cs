using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class ThingOwnerUtility_Patch
    {
        //public static Dictionary<int, List<IThingHolder>> tmpHoldersDict = new Dictionary<int, List<IThingHolder>>();
        [ThreadStatic] public static Stack<IThingHolder> tmpStack;
        [ThreadStatic] public static List<IThingHolder> tmpHolders;
        [ThreadStatic] public static List<Thing> tmpThings;
        [ThreadStatic] public static List<IThingHolder> tmpMapChildHolders;

        internal static void RunDestructivePatches()
        {
            var original = typeof(ThingOwnerUtility);
            var patched = typeof(ThingOwnerUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(AppendThingHoldersFromThings));
            RimThreadedHarmony.Prefix(original, patched, nameof(GetAllThingsRecursively),
                new[] {typeof(IThingHolder), typeof(List<Thing>), typeof(bool), typeof(Predicate<IThingHolder>)});
            var methods = original.GetMethods();
            MethodInfo GetAllThingsRecursivelyT = null;
            //MethodInfo originalPawnGetAllThings = original.GetMethod("GetAllThingsRecursively", bf, null, new Type[] { 
            //	typeof(Map), typeof(ThingRequest), typeof(List<Pawn>), typeof(bool), typeof(Predicate<IThingHolder>), typeof(bool) }, null);
            foreach (var method in methods)
                if (method.ToString()
                    .Equals(
                        "Void GetAllThingsRecursively[T](Verse.Map, Verse.ThingRequest, System.Collections.Generic.List`1[T], Boolean, System.Predicate`1[Verse.IThingHolder], Boolean)"))
                {
                    GetAllThingsRecursivelyT = method;
                    break;
                }

            //MethodInfo originalPawnGetAllThings = methods[17];
            var originalPawnGetAllThingsGeneric = GetAllThingsRecursivelyT.MakeGenericMethod(typeof(Pawn));
            var patchedPawnGetAllThings = patched.GetMethod(nameof(GetAllThingsRecursively_Pawn));
            var prefixPawnGetAllThings = new HarmonyMethod(patchedPawnGetAllThings);
            RimThreadedHarmony.harmony.Patch(originalPawnGetAllThingsGeneric, prefixPawnGetAllThings);

            //MethodInfo originalThingGetAllThings = methods[17];
            var originalThingGetAllThingsGeneric = GetAllThingsRecursivelyT.MakeGenericMethod(typeof(Thing));
            var patchedThingGetAllThings = patched.GetMethod(nameof(GetAllThingsRecursively_Thing));
            var prefixThingGetAllThings = new HarmonyMethod(patchedThingGetAllThings);
            RimThreadedHarmony.harmony.Patch(originalThingGetAllThingsGeneric, prefixThingGetAllThings);
        }

        public static bool AppendThingHoldersFromThings(List<IThingHolder> outThingsHolders, IList<Thing> container)
        {
            if (container == null) return false;
            var i = 0;
            var count = container.Count;
            Thing thing;
            while (i < count)
            {
                try
                {
                    thing = container[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }

                var thingHolder = thing as IThingHolder;
                if (thingHolder != null)
                    lock (outThingsHolders)
                    {
                        outThingsHolders.Add(thingHolder);
                    }

                var thingWithComps = container[i] as ThingWithComps;
                if (thingWithComps != null)
                {
                    var allComps = thingWithComps.AllComps;
                    for (var j = 0; j < allComps.Count; j++)
                    {
                        var thingHolder2 = allComps[j] as IThingHolder;
                        if (thingHolder2 != null)
                            lock (outThingsHolders)
                            {
                                outThingsHolders.Add(thingHolder2);
                            }
                    }
                }

                i++;
            }

            return false;
        }

        public static bool GetAllThingsRecursively(IThingHolder holder, List<Thing> outThings, bool allowUnreal = true,
            Predicate<IThingHolder> passCheck = null)
        {
            outThings.Clear();
            if (passCheck != null && !passCheck(holder)) return false;
            //Stack<IThingHolder> tmpStack = new Stack<IThingHolder>();
            tmpStack.Push(holder);
            while (tmpStack.Count != 0)
            {
                var thingHolder = tmpStack.Pop();
                if (allowUnreal || ThingOwnerUtility.AreImmediateContentsReal(thingHolder))
                {
                    var directlyHeldThings = thingHolder.GetDirectlyHeldThings();
                    if (directlyHeldThings != null) outThings.AddRange(directlyHeldThings);
                }

                //List<IThingHolder> tmpHolders = tmpHoldersDict[Thread.CurrentThread.ManagedThreadId];
                //List<IThingHolder> tmpHolders = new List<IThingHolder>();
                tmpHolders.Clear();
                thingHolder.GetChildHolders(tmpHolders);
                for (var i = 0; i < tmpHolders.Count; i++)
                    if (passCheck == null || passCheck(tmpHolders[i]))
                        tmpStack.Push(tmpHolders[i]);
            }

            tmpStack.Clear();
            tmpHolders.Clear();
            return false;
        }

        public static bool GetAllThingsRecursively_Pawn(Map map,
            ThingRequest request, List<Pawn> outThings, bool allowUnreal = true,
            Predicate<IThingHolder> passCheck = null, bool alsoGetSpawnedThings = true)
        {
            lock (outThings)
            {
                outThings.Clear();
            }

            if (alsoGetSpawnedThings)
            {
                var list = map.listerThings.ThingsMatching(request);
                for (var i = 0; i < list.Count; i++)
                {
                    var t = list[i] as Pawn;
                    if (t != null)
                        lock (outThings)
                        {
                            outThings.Add(t);
                        }
                }
            }

            //List<IThingHolder> tmpMapChildHolders = new List<IThingHolder>();
            tmpMapChildHolders.Clear();
            map.GetChildHolders(tmpMapChildHolders);
            for (var j = 0; j < tmpMapChildHolders.Count; j++)
            {
                tmpThings.Clear();
                //List<Thing> tmpThings = new List<Thing>();
                ThingOwnerUtility.GetAllThingsRecursively(tmpMapChildHolders[j], tmpThings, allowUnreal, passCheck);
                for (var k = 0; k < tmpThings.Count; k++)
                {
                    var t2 = tmpThings[k] as Pawn;
                    if (t2 != null && request.Accepts(t2))
                        lock (outThings)
                        {
                            outThings.Add(t2);
                        }
                }
            }

            tmpThings.Clear();
            tmpMapChildHolders.Clear();
            return false;
        }

        public static bool GetAllThingsRecursively_Thing(Map map, ThingRequest request, List<Thing> outThings,
            bool allowUnreal = true, Predicate<IThingHolder> passCheck = null, bool alsoGetSpawnedThings = true)
        {
            outThings.Clear();
            if (alsoGetSpawnedThings)
            {
                var list = map.listerThings.ThingsMatching(request);
                for (var i = 0; i < list.Count; i++)
                {
                    var val = list[i];
                    if (val != null) outThings.Add(val);
                }
            }

            //List<IThingHolder> tmpMapChildHolders = new List<IThingHolder>();
            tmpMapChildHolders.Clear();
            map.GetChildHolders(tmpMapChildHolders);
            //List<Thing> tmpThings = new List<Thing>();
            for (var j = 0; j < tmpMapChildHolders.Count; j++)
            {
                tmpThings.Clear();
                GetAllThingsRecursively(tmpMapChildHolders[j], tmpThings, allowUnreal, passCheck);
                for (var k = 0; k < tmpThings.Count; k++)
                    if (tmpThings[k] is Thing val2 && request.Accepts(val2))
                        outThings.Add(val2);
            }

            tmpThings.Clear();
            tmpMapChildHolders.Clear();
            return false;
        }
    }
}