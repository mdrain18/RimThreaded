using Verse;

namespace RimThreaded.RW_Patches
{
    internal class RegionLinkDatabase_Patch
    {
        public static void RunDestructivePatches()
        {
            var original = typeof(RegionLinkDatabase);
            var patched = typeof(RegionLinkDatabase_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(LinkFrom));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_LinkHasNoRegions));
        }

        public static bool LinkFrom(RegionLinkDatabase __instance, ref RegionLink __result, EdgeSpan span)
        {
            var key = span.UniqueHashCode();
            RegionLink value;
            var links = __instance.links;
            lock (links)
            {
                if (!links.TryGetValue(key, out value))
                {
                    value = new RegionLink();
                    value.span = span;
                    links.Add(key, value);
                }
            }

            __result = value;
            return false;
        }

        public static bool Notify_LinkHasNoRegions(RegionLinkDatabase __instance, RegionLink link)
        {
            var links = __instance.links;
            lock (links)
            {
                links.Remove(link.UniqueHashCode());
            }

            return false;
        }
    }
}