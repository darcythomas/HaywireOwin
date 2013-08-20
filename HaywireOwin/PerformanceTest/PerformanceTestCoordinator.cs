using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;
using MemoryMapBridgeProxy;

namespace TestPerformance
{
  public  class PerformanceTestCoordinator: IDisposable
    {

      public IHaywireBridge Bridge { get; private set; }
      private Process _minionProcess;
      private const string FileName = "PerformanceTest";

      public  PerformanceTestCoordinator()
      {
          Bridge = new HaywireBridge(FileName, HaywireStartUpMode.PerformanceTest);
      }

    //public PerformanceTestCoordinator(IHaywireBridge bridge)
    //{
    //    Bridge = bridge;
    //}
      
     

      public void RunTests()
      {
          StartOtherSideOfTheBridge();

          TestOneMillion test = new TestOneMillion();


          //get ineumenab of Itests. use MEF?
         TestResult result =  test.RunTest(Bridge);


         File.AppendAllLines(@"..\..\TestResults\TestResults.txt", new [] { result.ToJson()});
          Console.WriteLine(    result.ToString());

          Console.ReadLine();

          //run each test in turn returning Iresult
          //Write tests to log file
      }

      private void StartOtherSideOfTheBridge()
      {
          String args = String.Format("{0} {1}", FileName, HaywireStartUpMode.PerformanceTest);
          _minionProcess = Process.Start(@"..\..\..\MinionTestApp\bin\Debug\Minion.exe", args);
      }


      public void Dispose()
      {
          if (Bridge != null)
          {
              Bridge.Dispose();
          }



          if (_minionProcess != null && !_minionProcess.HasExited)
          {
              _minionProcess.Kill();
          }
      }
    }
}
