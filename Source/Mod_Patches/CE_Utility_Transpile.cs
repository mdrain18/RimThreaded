using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.Mod_Patches
{
    public class CE_Utility_Transpile
    {
        private static readonly Func<object[], object> safeFunction = parameters =>
            SafeBlit(
                (Texture2D) parameters[0],
                (Rect) parameters[1],
                (int[]) parameters[2]);

        public static IEnumerable<CodeInstruction> BlitCrop(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var currentInstructionIndex = 0;
            var matchFound = 0;
            while (currentInstructionIndex < instructionsList.Count)
            {
                var codeInstruction = instructionsList[currentInstructionIndex];
                if (codeInstruction.opcode == OpCodes.Call &&
                    (MethodInfo) codeInstruction.operand ==
                    AccessTools.Method(CombatExteneded_Patch.combatExtendedCE_Utility, "Blit"))
                {
                    matchFound++;
                    codeInstruction.operand = AccessTools.Method(typeof(CE_Utility_Transpile), "Blit");
                }

                yield return codeInstruction;
                currentInstructionIndex++;
            }

            if (matchFound < 1) Log.Error("IL code instructions not found");
        }

        public static IEnumerable<CodeInstruction> GetColorSafe(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            var instructionsList = instructions.ToList();
            var currentInstructionIndex = 0;
            var matchFound = 0;
            while (currentInstructionIndex < instructionsList.Count)
            {
                var codeInstruction = instructionsList[currentInstructionIndex];
                if (codeInstruction.opcode == OpCodes.Call && codeInstruction.operand is MethodInfo methodInfo &&
                    methodInfo == AccessTools.Method(CombatExteneded_Patch.combatExtendedCE_Utility, "Blit"))
                {
                    matchFound++;
                    codeInstruction.operand = AccessTools.Method(typeof(CE_Utility_Transpile), "Blit");
                }

                yield return codeInstruction;
                currentInstructionIndex++;
            }

            if (matchFound < 1) Log.Error("IL code instructions not found");
        }

        public static Texture2D Blit(Texture2D texture, Rect blitRect, int[] rtSize)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return SafeBlit(texture, blitRect, rtSize);
            threadInfo.safeFunctionRequest = new object[] {safeFunction, new object[] {texture, blitRect, rtSize}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (Texture2D) threadInfo.safeFunctionResult;
        }

        public static Texture2D SafeBlit(Texture2D texture, Rect blitRect, int[] rtSize)
        {
            var filterMode = texture.filterMode;
            texture.filterMode = FilterMode.Point;
            var temporary = RenderTexture.GetTemporary(rtSize[0], rtSize[1], 0, RenderTextureFormat.Default,
                RenderTextureReadWrite.Default, 1);
            temporary.filterMode = FilterMode.Point;
            RenderTexture.active = temporary;
            Graphics.Blit(texture, temporary);
            var texture2D = new Texture2D((int) blitRect.width, (int) blitRect.height);
            texture2D.ReadPixels(blitRect, 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
            texture.filterMode = filterMode;
            return texture2D;
        }
    }
}