using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MemoryMapBridgeProxy;

namespace TestPerformance
{
    class Program
    {
        static void Main(string[] args)
        {
            using (PerformanceTestCoordinator test = new PerformanceTestCoordinator())
            {
                test.RunTests();
            }
        }



    }

    public class PerformanceTest
    {

        public void RunTest()
        {
            using (HaywireBridge Sender = new HaywireBridge("PerformanceTest", HaywireStartUpMode.PerformanceTest))
            using (HaywireBridge Receiver = new HaywireBridge("PerformanceTest", HaywireStartUpMode.Debug))
            {
                const int loops = 1000000;

                Stopwatch sw = new Stopwatch();
                Sender.RaiseEvent(-1);//warm up

                sw.Start();
                for (int i = 0; i < loops; i++)
                {
                    Sender.RaiseEvent(i);
                }


                sw.Stop();


                Assembly assembly = Assembly.LoadFrom("MemoryMapBridgeProxy.dll");
                String version = assembly.GetName().Version.ToString();

                Console.WriteLine("{0} loops in {1}ms, {2:N0}/sec. -- Version: {3} -- {4:u}", loops,
                    sw.ElapsedMilliseconds, loops * TimeSpan.TicksPerSecond/(1.0 * sw.ElapsedTicks), version,
                    DateTime.Now);

                Console.ReadLine();
            }
        }

    }
}
