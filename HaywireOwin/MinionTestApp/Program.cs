using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MemoryMapBridgeProxy;

namespace Minion
{
    class Program
    {
        static void Main(string[] args)
        {
            Mutex shutDownSynchMutex = new Mutex(false, "haywireShutDownSyncMutex");



            using (HaywireBridge hb1 = new HaywireBridge("coffee"))
            {
                try
                {
                    //Mutex used to close minion app when the main debug app is closed
                    shutDownSynchMutex.WaitOne();
                    shutDownSynchMutex.ReleaseMutex();
                }
                catch (AbandonedMutexException)
                {
                    Console.WriteLine("Shutting Down");
                   //swallow
                }
               
            }

           
        }
    }
}
