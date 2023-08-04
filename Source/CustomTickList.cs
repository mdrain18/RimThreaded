using System;
using System.Threading;

namespace RimThreaded
{
    public class ThreadedTickList
    {
        public Action prepareAction;
        public int preparing = -1;
        public EventWaitHandle prepEventWaitStart = new ManualResetEvent(false);
        public bool readyToTick = false;
        public int threadCount = -1;
        public Action tickAction;
    }
}