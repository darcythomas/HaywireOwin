using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MemoryMapBridgeProxy;

namespace TestPerformance
{
   // [Export(typeof(IPerformanceTest))]
   public class TestOneMillion : IPerformanceTest
   {
       public TestResult RunTest(IHaywireBridge sender)
       {
           const int loops = 1000000;

           Stopwatch sw = new Stopwatch();
           sender.RaiseEvent(-1);//warm up

           sw.Start();
           for (int i = 0; i < loops; i++)
           {
               sender.RaiseEvent(i);
           }


           sw.Stop();



           Version version = sender.Version;


           return new TestResult()
           {
               HaywireVersion = version,
               TestName = MethodInfo.GetCurrentMethod().DeclaringType.Name,
               RunDate = DateTime.Now,
               TimeTaken = new TimeSpan(sw.ElapsedTicks),
               Transactions = loops
           };
           Console.WriteLine("{0} loops in {1}ms, {2:N0}/sec. -- Version: {3} -- {4:u}", loops,
               sw.ElapsedMilliseconds, loops * TimeSpan.TicksPerSecond / (1.0 * sw.ElapsedTicks), version,
               DateTime.Now);


       }
   }
}
