using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld.Planet;
using Verse;

namespace RimThreaded.RW_Patches
{
    //Class was largely overhauled to allow multithreaded ticking for WorldPawns.Tick()
    public class WorldComponentUtility_Patch
    {
        public static List<WorldComponent> worldComponents;
        public static int worldComponentTicks;

        public static bool WorldComponentTick(World world)
        {
            worldComponents = world.components;
            worldComponentTicks = worldComponents.Count;
            return false;
        }

        internal static void RunDestructivePatches()
        {
            var original = typeof(WorldComponentUtility);
            var patched = typeof(WorldComponentUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, "WorldComponentTick");
        }

        public static void WorldComponentPrepare()
        {
            try
            {
                var world = Find.World;
                world.debugDrawer.WorldDebugDrawerTick();
                world.pathGrid.WorldPathGridTick();
                WorldComponentUtility.WorldComponentTick(world);
            }
            catch (Exception ex3)
            {
                Log.Error(ex3.ToString());
            }
        }

        public static void WorldComponentListTick()
        {
            while (true)
            {
                var index = Interlocked.Decrement(ref worldComponentTicks);
                if (index < 0) return;
                var worldComponent = worldComponents[index];
                if (null != worldComponent) //TODO: is null-check and lock necessary?
                    try
                    {
                        worldComponent.WorldComponentTick();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception ticking World Component: " + worldComponent.ToStringSafe() + ex);
                    }
            }
        }
    }
}