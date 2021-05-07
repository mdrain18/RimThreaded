﻿using System;
using UnityEngine;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{

    public class Graphics_Patch
	{
        static readonly Action<object[]> safeFunction = p =>
            Graphics.Blit((Texture)p[0], (RenderTexture)p[1]);

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Graphics);
            Type patched = typeof(Graphics_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Blit", new Type[] { typeof(Texture), typeof(RenderTexture) });
            RimThreadedHarmony.Prefix(original, patched, "DrawMesh", new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int) });
        }
        public static bool Blit(Texture source, RenderTexture dest)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { source, dest } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

        static readonly Action<object[]> safeFunctionDrawMesh = p =>
        Graphics.DrawMesh((Mesh)p[0], (Vector3)p[1], (Quaternion)p[2], (Material)p[3], (int)p[4]);

        public static bool DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { safeFunctionDrawMesh, new object[] { mesh, position, rotation, material, layer } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

    }
}
