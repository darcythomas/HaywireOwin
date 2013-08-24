using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MemoryMapBridgeProxy;
using Newtonsoft.Json;

namespace TestPerformance
{
    [Export]
    public class PerformanceTestCoordinator : IDisposable
    {

        public IHaywireBridge Bridge { get; private set; }
        private Process _minionProcess;
        private readonly string _logFileName = @"..\..\TestResults\";
        private readonly String suffix ="-TestResults.txt";
        private const string FileName = "PerformanceTest";

        [ImportMany(typeof(IPerformanceTest))]
       IEnumerable<IPerformanceTest> performanceTests { get; set; }


        public PerformanceTestCoordinator()
        {
            Bridge = new HaywireBridge(FileName, HaywireStartUpMode.PerformanceTest);


            AggregateCatalog catalog = new AggregateCatalog();

            AssemblyCatalog assemblyCatalog = new AssemblyCatalog(typeof(Program).Assembly);
          //  DirectoryCatalog directoryCatalog = new DirectoryCatalog(".", "Library*.dll");
          //  catalog.Catalogs.Add(directoryCatalog);
            catalog.Catalogs.Add(assemblyCatalog);

            this.Container = new CompositionContainer(catalog);

            //CompositionBatch batch = new CompositionBatch();
            //batch.AddExportedValue(this.Container);

           // this.Container.Compose(batch);
            this.Container.ComposeParts(this);

        }

        public CompositionContainer Container { get; set; }

        //public PerformanceTestCoordinator(IHaywireBridge bridge)
        //{
        //    Bridge = bridge;
        //}



        public void RunTests()
        {
            Mutex shutDownSynchMutex = new Mutex(true, "haywireShutDownSyncMutex");

            StartOtherSideOfTheBridge();

            Console.WriteLine("Attach debugger now");
            Console.ReadLine();


            //var q = performanceTests.Value;
            foreach (var performanceTest in performanceTests)
            {
                var q = Enumerable.Range(0, 10).Select(s => performanceTest.RunTest(Bridge)).ToList();
                TestResult result =  q.First();
                result.TimeTaken = new TimeSpan(Convert.ToInt64( q.Average(a => a.TimeTaken.Ticks)));
                //result.TimeTaken = new TimeSpan(Convert.ToInt64(q.Average(a => a.TimeTaken.Ticks)));
               // TestResult result = performanceTest.RunTest(Bridge);

                IncludeLastRunsData(result);
                WriteToHeadOfLogFile(result);

                Console.WriteLine(result.ToString());
                foreach (var testResult in q)
                {
                    Console.WriteLine(testResult);
                }
            }
           

          


            Console.ReadLine();
            shutDownSynchMutex.ReleaseMutex();
        }

        private void IncludeLastRunsData(TestResult result)
        {
            File.Open(_logFileName + result.TestName + suffix,FileMode.OpenOrCreate).Dispose();
            var lastRun = JsonConvert.DeserializeObject<TestResult>(File.ReadLines(_logFileName+ result.TestName + suffix).FirstOrDefault()??String.Empty);
            if (lastRun != null)
            {
                result.LastRun = lastRun.TransactionsPerSecond;
            }
        }

        private void WriteToHeadOfLogFile(TestResult result)
        {
            IEnumerable<String> latestResult = new[] { result.ToJson() };

            String logFileName = _logFileName + result.TestName + suffix;

            IEnumerable<String> currentRows = File.ReadLines(logFileName);
            IEnumerable<String> results = latestResult.Concat(currentRows);
            File.AppendAllLines(logFileName  + ".new", results);
            File.Replace(logFileName + ".new", logFileName, logFileName + ".old");
            File.Delete(logFileName + ".old");
            File.Delete(logFileName + ".new");
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
