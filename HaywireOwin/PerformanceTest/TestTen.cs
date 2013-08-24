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
    [Export(typeof(IPerformanceTest))]
   public class TestTen : IPerformanceTest
   {
       public TestResult RunTest(IHaywireBridge sender)
       {
           const int loops = 10;

           Stopwatch sw = new Stopwatch();

           String message = "I need coffee & a bagel X ";
           sender.RaiseEvent(message,MessageType.Echo);//warm up

           sw.Start();
           for (int i = 0; i < loops; i++)
           {
               sender.RaiseEvent(message + i.ToString(), MessageType.Echo);
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
          


       }
   }
}
