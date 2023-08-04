﻿using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    public static class Rand_Patch
    {
        public static object lockObject = new object();

        internal static void RunDestructivePatches()
        {
            var original = typeof(Rand);
            var patched = typeof(Rand_Patch);
            RimThreadedHarmony.Prefix(original, patched, "PushState", Type.EmptyTypes);
            RimThreadedHarmony.Prefix(original, patched, "PopState");
        }

        public static bool PushState()
        {
            lock (lockObject)
            {
                Rand.stateStack.Push(Rand.StateCompressed);
            }

            return false;
        }

        public static bool PopState()
        {
            lock (lockObject)
            {
                Rand.StateCompressed = Rand.stateStack.Pop();
            }

            return false;
        }
    }
}