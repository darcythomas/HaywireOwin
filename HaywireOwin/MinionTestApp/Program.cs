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

            String filename = HaywireBridge.DefaultMemorymappedFileName;
            HaywireStartUpMode startUpMode  = HaywireStartUpMode.Default;
             
            if (args.Any())
            {
                filename = args[0];
                Enum.TryParse(args[1], true, out startUpMode);
            }

            using (HaywireBridge hb1 = new HaywireBridge(filename, startUpMode))
            {
                try
                {
                    Console.WriteLine("Minion Runner Started running HaywireBridge version {0}", hb1.Version);

                    //Mutex used to close minion app when a parent debug app is closed
                    shutDownSynchMutex.WaitOne();
                    shutDownSynchMutex.ReleaseMutex();
                }
                catch (AbandonedMutexException)
                {
                    //swallow
                    Console.WriteLine("Shutting Down");
                }
            }
        }
    }
}
