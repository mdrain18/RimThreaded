using System.Collections.Concurrent;

namespace RimThreaded.RW_Patches
{
    public static class SimplePool_Patch<T> where T : new()
    {
        private static readonly ConcurrentStack<T> FreeItems = new ConcurrentStack<T>();

        public static int FreeItemsCount => FreeItems.Count;

        public static T Get()
        {
            return FreeItems.TryPop(out var freeItem) ? freeItem : new T();
        }

        public static void Return(T item)
        {
            FreeItems.Push(item);
            //as a precaution this might require a check for duplicates.
        }
    }
}