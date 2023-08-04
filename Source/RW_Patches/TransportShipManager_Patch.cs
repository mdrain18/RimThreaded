﻿using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    internal class TransportShipManager_Patch
    {
        public static List<TransportShip> AllTransportShips;
        public static int AllTransportShipsCount;

        internal static void RunNonDestructivePatches()
        {
            var original = typeof(TransportShipManager);
            //Type patched = typeof(TransportShipManager_Patch);
            RimThreadedHarmony.TranspileMethodLock(original, "RegisterShipObject");
            RimThreadedHarmony.TranspileMethodLock(original, "DeregisterShipObject");
        }

        public static void ShipObjectsPrepare()
        {
            AllTransportShips = Current.Game.transportShipManager.ships;
            AllTransportShipsCount = AllTransportShips.Count;
        }

        public static void ShipObjectsTick()
        {
            while (true)
            {
                var index = Interlocked.Decrement(ref AllTransportShipsCount);
                if (index < 0) return;
                try
                {
                    AllTransportShips[index].Tick();
                }
                catch (Exception e)
                {
                    Log.Error("Exception ticking TransportShip: " + AllTransportShips[index] + ": " + e);
                }
                /*
                                int index = Interlocked.Decrement(ref allFactionsTicks);
                if (index < 0) return;
                Faction faction = allFactionsTickList[index];
                try
                {
                    faction.FactionTick();
                }
                catch (Exception ex)
                {
                    Log.Error("Exception ticking faction: " + faction.ToStringSafe() + ": " + ex);
                }
                }*/
            }
        }
    }
}