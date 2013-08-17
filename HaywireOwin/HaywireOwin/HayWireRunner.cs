using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MemoryMapBridgeProxy;


namespace HaywireOwinTechSpike
{
    public class HayWireRunner : IThreadRunner
    {
        private static bool _stillRunning = true;
        private static Thread _runningThread;
        private static Process _minionProcess;

        public void Init()
        {


        }

        public void Run()
        {
            _runningThread = new Thread(HaywireThread);
            _runningThread.Start();

            _minionProcess = Process.Start(@"..\..\..\Minion\bin\Debug\Minion.exe");
        }

        private void HaywireThread()
        {
            using (HaywireBridge hb1 = new HaywireBridge("coffee"))
            {
                int m = 1;
                while (_stillRunning)
                {
                    var message = Console.ReadLine();
                    if (message != null && message.Equals("quit", StringComparison.InvariantCultureIgnoreCase)) break;

                    hb1.RaiseEvent(m);

                    m++;
                }
            }
        }

        public static void ShutDown()
        {
            _stillRunning = false;
            if (_minionProcess != null)
            {
                _minionProcess.Kill();
            }


            if (_runningThread != null)
            {
                _runningThread.Join(1000);
            }

            if (_minionProcess == null) return;
            while (_minionProcess.HasExited)
            {
                Thread.Sleep(100);
            }
        }

        public void Dispose()
        {
            ShutDown();
        }
    }
}
