using System.Collections.Generic;
using RimThreaded;
using Verse;

public class BodyDef_Patch
{
    internal static void RunDestructivePatches()
    {
        var original = typeof(BodyDef);
        var patched = typeof(BodyDef_Patch);
        RimThreadedHarmony.Prefix(original, patched, nameof(GetPartsWithTag));
    }

    public static bool GetPartsWithTag(BodyDef __instance, ref List<BodyPartRecord> __result, BodyPartTagDef tag)
    {
        var cachedPartsByTag = __instance.cachedPartsByTag;

        if (cachedPartsByTag.TryGetValue(tag, out __result))
            return false;

        lock (cachedPartsByTag)
        {
            if (cachedPartsByTag.TryGetValue(tag, out __result))
                return false;
            var AllParts = __instance.AllParts;
            __result = new List<BodyPartRecord>();
            for (var i = 0; i < AllParts.Count; i++)
            {
                var bodyPartRecord = AllParts[i];
                if (bodyPartRecord.def.tags.Contains(tag)) __result.Add(bodyPartRecord);
            }

            cachedPartsByTag[tag] = __result;
        }

        return false;
    }
}