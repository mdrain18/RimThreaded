using System;
using System.Threading;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class TradeShip_Patch
    {
        public static int totalTradeShipsCount;
        public static int totalTradeShipTicks;
        public static int totalTradeShipTicksCompleted;
        public static TradeShipStructure[] tradeShips = new TradeShipStructure[99];

        internal static void RunDestructivePatches()
        {
            var original = typeof(TradeShip);
            var patched = typeof(TradeShip_Patch);
            RimThreadedHarmony.Prefix(original, patched, "PassingShipTick");
        }

        public static bool PassingShipTick(TradeShip __instance)
        {
            --__instance.ticksUntilDeparture;
            if (__instance.Departed)
                __instance.Depart();
            var index = Interlocked.Increment(ref totalTradeShipsCount) - 1;
            var thingsOwner = __instance.things;
            tradeShips[index].TradeShipThings = thingsOwner;
            Interlocked.Add(ref totalTradeShipTicks, thingsOwner.Count);
            tradeShips[index].TradeShipTicks = totalTradeShipTicks;
            return false;
        }

        public static void PassingShipListTick()
        {
            while (true)
            {
                var ticketIndex = Interlocked.Increment(ref totalTradeShipTicksCompleted) - 1;
                if (ticketIndex >= totalTradeShipTicks) return;
                var totalTradeShipIndex = 0;
                while (ticketIndex < totalTradeShipTicks)
                {
                    var index = ticketIndex;
                    while (ticketIndex >= tradeShips[totalTradeShipIndex].TradeShipTicks) totalTradeShipIndex++;
                    if (totalTradeShipIndex > 0)
                        index = ticketIndex - tradeShips[totalTradeShipIndex - 1].TradeShipTicks;
                    var thingOwner = tradeShips[totalTradeShipIndex].TradeShipThings;
                    var thing = thingOwner.GetAt(index);
                    if (thing is Pawn pawn)
                    {
                        try
                        {
                            pawn.Tick();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Exception ticking Pawn: " + pawn.ToStringSafe() + " " + ex);
                        }

                        if (pawn.Dead)
                            lock (thingOwner)
                            {
                                thingOwner.Remove(pawn);
                            }
                    }

                    ticketIndex = Interlocked.Increment(ref totalTradeShipTicksCompleted) - 1;
                }
            }
        }

        public struct TradeShipStructure
        {
            public int TradeShipTicks;
            public ThingOwner TradeShipThings;
        }
    }
}