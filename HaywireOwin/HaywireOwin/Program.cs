using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MemoryMapBridgeProxy;

namespace HaywireOwinTechSpike
{
    class Program
    {
        private static bool _stillRunning = true;
        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Application.ApplicationExit += CurrentDomain_ProcessExit;

            Mutex shutDownSynchMutex = new Mutex(true,"haywireShutDownSyncMutex");

            using (HayWireRunner runner = new HayWireRunner())
            {
                runner.Run();

                while (_stillRunning)
                {
                    Thread.Yield();
                }
            }

            shutDownSynchMutex.ReleaseMutex();
            return 0;

        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            HayWireRunner.ShutDown();
            _stillRunning = false;
            Thread.Sleep(500);
        }
    }
}
