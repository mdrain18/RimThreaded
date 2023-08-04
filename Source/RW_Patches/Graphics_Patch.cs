﻿using System;
using UnityEngine;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    public class Graphics_Patch
    {
        private static readonly Action<object[]> ActionGraphicsBlit = p =>
            Graphics.Blit((Texture) p[0], (RenderTexture) p[1]);


        private static readonly Action<object[]> ActionGraphicsDrawMesh = p =>
            Graphics.DrawMesh((Mesh) p[0], (Vector3) p[1], (Quaternion) p[2], (Material) p[3], (int) p[4]);


        private static readonly Action<object[]> ActionGraphicsDrawMeshNow = p =>
            Graphics.DrawMeshNow((Mesh) p[0], (Vector3) p[1], (Quaternion) p[2]);


        internal static void RunDestructivePatches()
        {
            var original = typeof(Graphics);
            var patched = typeof(Graphics_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Blit", new[] {typeof(Texture), typeof(RenderTexture)});
            RimThreadedHarmony.Prefix(original, patched, "DrawMesh",
                new[] {typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int)});
            RimThreadedHarmony.Prefix(original, patched, "DrawMeshNow",
                new[] {typeof(Mesh), typeof(Vector3), typeof(Quaternion)});
        }

        public static bool Blit(Texture source, RenderTexture dest)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] {ActionGraphicsBlit, new object[] {source, dest}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

        public static bool DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[]
                {ActionGraphicsDrawMesh, new object[] {mesh, position, rotation, material, layer}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

        public static bool DrawMeshNow(Mesh mesh, Vector3 position, Quaternion rotation)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out var threadInfo))
                return true;

            threadInfo.safeFunctionRequest = new object[]
                {ActionGraphicsDrawMeshNow, new object[] {mesh, position, rotation}};
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
    }
}