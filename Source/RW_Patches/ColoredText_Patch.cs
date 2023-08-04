using System;
using System.Text.RegularExpressions;
using RimWorld;

namespace RimThreaded.RW_Patches
{
    internal class ColoredText_Patch
    {
        [ThreadStatic] public static Regex ColonistCountRegex;

        public static void InitializeThreadStatics()
        {
            ColonistCountRegex = new Regex("\\d+\\.?\\d* " + "(" + FactionDefOf.PlayerColony.pawnsPlural + "|" +
                                           FactionDefOf.PlayerColony.pawnSingular + ")");
        }
    }
}