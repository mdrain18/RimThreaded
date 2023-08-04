using RimWorld;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.Mod_Patches
{
    internal class Fluffy_Breakdowns_Patch
    {
        public static void Patch()
        {
            var MapComponent_Durability =
                TypeByName(
                    "Fluffy_Breakdowns.MapComponent_Durability"); //Fluffy_Breakdowns.MapComponent_Durability.GetDurability
            if (MapComponent_Durability != null)
            {
                var methodName = "GetDurability";
                Log.Message("RimThreaded is patching " + MapComponent_Durability.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(MapComponent_Durability, methodName, LockFlag.WriterLock,
                    new[] {typeof(CompBreakdownable)});
                methodName = "ExposeData";
                Log.Message("RimThreaded is patching " + MapComponent_Durability.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(MapComponent_Durability, methodName, LockFlag.WriterLock);
                methodName = "MapComponentTick";
                Log.Message("RimThreaded is patching " + MapComponent_Durability.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(MapComponent_Durability, methodName, LockFlag.WriterLock);
                methodName = "SetDurability";
                Log.Message("RimThreaded is patching " + MapComponent_Durability.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(MapComponent_Durability, methodName, LockFlag.WriterLock,
                    new[] {typeof(CompBreakdownable), typeof(float)});
            }
        }
    }
}