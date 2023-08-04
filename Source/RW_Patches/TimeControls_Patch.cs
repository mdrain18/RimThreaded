using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimThreaded.RW_Patches
{
    public class TimeControls_Patch
    {
        public static bool lastTickForcedSlow;
        public static bool overrideForcedSlow;


        internal static void RunNonDestructivePatches()
        {
            var original = typeof(TimeControls);
            var patched = typeof(TimeControls_Patch);
            RimThreadedHarmony.Prefix(original, patched, "DoTimeControlsGUI", new[] {typeof(Rect)}, false);
        }

        public static bool DoTimeControlsGUI(Rect timerRect)
        {
            if (Event.current.type != EventType.KeyDown) return true;

            if (!Find.WindowStack.WindowsForcePause)
                if (KeyBindingDefOf.TimeSpeed_Fast.KeyDownEvent ||
                    KeyBindingDefOf.TimeSpeed_Superfast.KeyDownEvent ||
                    KeyBindingDefOf.TimeSpeed_Ultrafast.KeyDownEvent
                   )
                    if (lastTickForcedSlow)
                        overrideForcedSlow = true;

            //allow speed 4 even if not dev mode
            if (Prefs.DevMode)
                return true;
            var tickManager = Find.TickManager;
            if (KeyBindingDefOf.TimeSpeed_Ultrafast.KeyDownEvent)
            {
                tickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
                TimeControls.PlaySoundOf(tickManager.CurTimeSpeed);
                Event.current.Use();
            }

            if (!KeyBindingDefOf.Dev_TickOnce.KeyDownEvent || tickManager.CurTimeSpeed != TimeSpeed.Paused)
                return true;
            tickManager.DoSingleTick();
            SoundDefOf.Clock_Stop.PlayOneShotOnCamera();

            return true;
        }
    }
}