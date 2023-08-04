using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class IdeoManager_Patch
    {
        public static List<Ideo> Ideos;
        public static int IdeosCount;

        internal static void RunNonDestructivePatches() //there may be the need for locks in the IdeoManager
        {
            var original = typeof(IdeoManager);
        }

        public static void IdeosPrepare()
        {
            Ideos = Current.Game.World.ideoManager.ideos;
            IdeosCount = Ideos.Count;
        }

        public static void IdeosTick()
        {
            while (true)
            {
                var index = Interlocked.Decrement(ref IdeosCount);
                if (index < 0) return;
                try
                {
                    Ideos[index].IdeoTick();
                }
                catch (Exception e)
                {
                    Log.Error("Exception ticking Ideo: " + Ideos[index] + ": " + e);
                }
            }
        }
    }
}