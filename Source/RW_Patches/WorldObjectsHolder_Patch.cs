using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld.Planet;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class WorldObjectsHolder_Patch
    {
        public static List<WorldObject> worldObjectsTickList;

        public static int worldObjectsTicks;

        //Class was largely overhauled to allow multithreaded ticking for WorldPawns.Tick()
        internal static void RunDestructivePatches()
        {
            var original = typeof(WorldObjectsHolder);
            var patched = typeof(WorldObjectsHolder_Patch);
            RimThreadedHarmony.Prefix(original, patched, "WorldObjectsHolderTick");
        }

        public static bool WorldObjectsHolderTick(WorldObjectsHolder __instance)
        {
            worldObjectsTickList = __instance.worldObjects;
            worldObjectsTicks = __instance.worldObjects.Count;
            return false;
        }

        public static void WorldObjectsPrepare()
        {
            try
            {
                var world = Find.World;
                world.worldObjects.WorldObjectsHolderTick();
            }
            catch (Exception ex3)
            {
                Log.Error(ex3.ToString());
            }
        }

        public static void WorldObjectsListTick()
        {
            while (true)
            {
                var index = Interlocked.Decrement(ref worldObjectsTicks);
                if (index < 0) return;
                var worldObject = worldObjectsTickList[index];
                try
                {
                    worldObject.Tick();
                }
                catch (Exception ex)
                {
                    Log.Error("Exception ticking world object: " + worldObject.ToStringSafe() + ": " + ex);
                }
            }
        }
    }
}