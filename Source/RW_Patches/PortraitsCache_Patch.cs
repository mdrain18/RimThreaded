using RimWorld;

namespace RimThreaded.RW_Patches
{
    public class PortraitsCache_Patch
    {
        internal static void RunDestructivePatches()
        {
            var original = typeof(PortraitsCache);
            var patched = typeof(PortraitsCache_Patch);
            /*
            RimThreadedHarmony.Prefix(original, patched, nameof(Clear));
            RimThreadedHarmony.Prefix(original, patched, nameof(GetOrCreateCachedPortraitsWithParams));
            RimThreadedHarmony.Prefix(original, patched, nameof(NewRenderTexture));
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveExpiredCachedPortraits));
            RimThreadedHarmony.Prefix(original, patched, nameof(Get));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetAnimatedPortraitsDirty));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetDirty));
            */
        }
    }
}