using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using MemoryMapBridgeProxy;
using NUnit.Framework;

namespace HayWireUnitTest
{


    [TestFixture]
    public class ExampleTestOfNUnit
    {

        private String _dataTestbasicHaywire = String.Empty;
       


        [Test]
        public void TestTheTester()
        {
            Assert.That(2 * 2, Is.EqualTo(4), "Testing framework works");
        }

        [Test]
        public void TestBasicHaywire()
        {

            const string orignalMessage = "Something original and witty";

            using (IHaywireBridge hwOut = new HaywireBridge())
            using (IHaywireBridge hwIn = new HaywireBridge())
            {
                hwOut.SubscribeToEvent(ProcessResponse, MessageType.TestRequest);
             //   Thread.Sleep(100);
                hwIn.RaiseEvent(orignalMessage,MessageType.TestRequest);
            }
          
           Thread.Sleep(100);//Give the processing thread a chance to complete
            Assert.That(_dataTestbasicHaywire, Is.EqualTo(orignalMessage), "Send a simple Text message");
        }


        [Test]
        public void TestBasicHaywire2()
        {

            const string orignalMessage2 = "Something less witty";

            using (IHaywireBridge hwOut = new HaywireBridge())
            using (IHaywireBridge hwIn = new HaywireBridge())
            {
                hwOut.SubscribeToEvent(ProcessResponse, MessageType.TestRequest);
                //Thread.Sleep(100);
                hwIn.RaiseEvent(orignalMessage2, MessageType.TestRequest);
            }

            Thread.Sleep(100);//Give the processing thread a chance to complete
            Assert.That(_dataTestbasicHaywire, Is.EqualTo(orignalMessage2), "Send a simple Text message");
        }

        [Test]
        public void TestBasicHaywire3()
        {
          var q =   Enumerable.Range(0, 100).ToList();
            foreach (var i in q)
            {
                LoopableTest(i.ToString());
            }
           
        }

        private void LoopableTest(String startingString)
        {
            _dataTestbasicHaywire = startingString;
            const string orignalMessage2 = "Something witless";

            using (IHaywireBridge hwOut = new HaywireBridge())
            using (IHaywireBridge hwIn = new HaywireBridge())
            {
                Debug.WriteLine("Pre subscribe");
                hwOut.SubscribeToEvent(ProcessResponse, MessageType.TestRequest);
                Thread.Sleep(1);
                Debug.WriteLine("Pre raise");
                hwIn.RaiseEvent(orignalMessage2, MessageType.TestRequest);
                Debug.WriteLine("Post raise");
            }
            Stopwatch sw = new Stopwatch();
            int count = 0;
            sw.Start();
            while (_dataTestbasicHaywire == startingString && count < 1000)
            {
                count++;
                Thread.Sleep(1); //Give the processing thread a chance to complete
            }
            sw.Stop();

            Debug.WriteLine("Time take to complete {0}ms, count {1}", sw.ElapsedMilliseconds, count);
            Assert.That(_dataTestbasicHaywire, Is.EqualTo(orignalMessage2), "Send a simple Text message");
        }

        private void ProcessResponse(EventCaller eventCaller, MemoryMappedViewAccessor accessor)
        {
           
            byte[] data = new byte[eventCaller.MessageLength];

            accessor.ReadArray(eventCaller.OffsetPosition, data, 0, eventCaller.MessageLength);

            _dataTestbasicHaywire = data.GetString();
        }

    }

}