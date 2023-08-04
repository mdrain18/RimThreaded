using RimThreaded.RW_Patches;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    internal class Better_Message_Placement_Patch
    {
        public static void Patch()
        {
            var Messages_MessagesDoGUI_Patch = TypeByName("Better_Message_Placement.Messages_MessagesDoGUI_Patch");
            if (Messages_MessagesDoGUI_Patch != null)
            {
                var methodName = "MessagesDoGUI";
                Log.Message("RimThreaded is patching " + typeof(Messages_Patch).FullName + " " + methodName);
                Transpile(typeof(Messages_Patch), Messages_MessagesDoGUI_Patch, methodName, patchMethod: "Transpiler");
            }
        }
    }
}