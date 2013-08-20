using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MemoryMapBridgeProxy
{

    public enum HaywireStartUpMode
    {
        Default = 0,
        Debug = 1,
        Normal = 2,
        PerformanceTest = 3
    }

   

    public class HaywireBridge : IHaywireBridge
    {
        //TODO: Look at using the fullnamespac.classname.version as the mutex string
        private const String SystemWideMutexName = "HaywireMemoryMappedFileSetupMutex";
        public  static readonly string DefaultMemorymappedFileName = "HaywireBridgeMemoryMap";
        private static readonly Mutex MemoryMapsetupMutex = new Mutex(false, SystemWideMutexName);
        private readonly ConcurrentQueue<EventCaller> _eventsToSendQueue = new ConcurrentQueue<EventCaller>();
        private ConcurrentQueue<Object> _eventsRecievedQueue = new ConcurrentQueue<Object>();
        private Thread _eventSpinnerThreadIn;
        private Thread _eventSpinnerThreadOut;
        private Boolean _eventSpinnerThreadsRunning = true;
        private Action<EventCaller> processEventDelegate;
        public String FileName { get; private set; }
        MemoryMappedFile _memoryMappedIn;
        MemoryMappedFile _memoryMappedOut;
        MemoryMappedFile PagedMemoryMapped;
        private readonly long BridgeSize = 1024 * 4;




        //Implement IOWIN
        //Event loop
        //Read string back
        // pseudo GC ring buffer (bonus: grow and shrink)
        //serialize dictionary
        //Stream
        //Create one memmap with access wrapped in a global mutex to write/read init coms to




        public HaywireBridge(String fileName = null, HaywireStartUpMode mode = HaywireStartUpMode.Default, MemoryMappedFileSecurity customSecurity = null)
        {
            SetDefaultIncomingEventProcessor(mode);
            SetMemoryMappedFileName(fileName);
            SetupMemoryMappedFileSecurity(customSecurity);
            SetupMemoryMappedFiles(customSecurity);
            StartEventspinners();
        }

        private static void SetupMemoryMappedFileSecurity(MemoryMappedFileSecurity customSecurity)
        {
            if (customSecurity != null) return;
            customSecurity = new MemoryMappedFileSecurity();
            customSecurity.AddAccessRule(new AccessRule<MemoryMappedFileRights>("everyone",
                MemoryMappedFileRights.FullControl, AccessControlType.Allow));
        }

        private void SetMemoryMappedFileName(String fileName)
        {
            FileName =  String.IsNullOrWhiteSpace(fileName)? DefaultMemorymappedFileName : fileName;
        }

        private void SetupMemoryMappedFiles(MemoryMappedFileSecurity customSecurity)
        {
            //TODO: look at just using a mutex to setup (remove try catch)
            MemoryMapsetupMutex.WaitOne();
            try
            {
                _memoryMappedIn = MemoryMappedFile.CreateNew(FileName + "-MasterInSlaveOut", 1024 * 4,
                    MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, customSecurity,
                    HandleInheritability.Inheritable);
            }
            catch (Exception)
            {
                _memoryMappedIn = MemoryMappedFile.CreateOrOpen(FileName + "-SlaveInMasterOut", 1024 * 4,
                    MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, customSecurity,
                    HandleInheritability.Inheritable);
            }

            try
            {
                _memoryMappedOut = MemoryMappedFile.CreateNew(FileName + "-SlaveInMasterOut", 1024 * 4,
                    MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, customSecurity,
                    HandleInheritability.Inheritable);
            }
            catch (Exception)
            {
                _memoryMappedOut = MemoryMappedFile.CreateOrOpen(FileName + "-MasterInSlaveOut", BridgeSize,
                    MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, customSecurity,
                    HandleInheritability.Inheritable);
            }

            MemoryMapsetupMutex.ReleaseMutex();
        }

        private void SetDefaultIncomingEventProcessor(HaywireStartUpMode mode)
        {

            switch (mode)
            {
                case HaywireStartUpMode.Default:
#if (DEBUG)
                    //If a debug release then use the debug processor, otherwise fall through and use the normal one
                    processEventDelegate = this.DebugProcessIncomingEvent;
                    break;
#endif
                case HaywireStartUpMode.Normal:
                    processEventDelegate = this.NormalProcessIncomingEvent;
                    break;
                case HaywireStartUpMode.Debug:
                    processEventDelegate = this.DebugProcessIncomingEvent;
                    break;
                case HaywireStartUpMode.PerformanceTest:
                    processEventDelegate = this.PerformanceTestProcessIncomingEvent;
                    break;
            }

        }

        private void NormalProcessIncomingEvent(EventCaller obj)
        {
            throw new NotImplementedException();
        }

        private void PerformanceTestProcessIncomingEvent(EventCaller obj)
        {
            //Do Nothing
        }

        public MemoryMappedViewAccessor GetInAccessor()
        {
            return _memoryMappedIn.CreateViewAccessor();
        }
        public MemoryMappedViewAccessor GetOutAccessor()
        {
            return _memoryMappedOut.CreateViewAccessor();
        }

        public void AddToQueue(int item)
        {
            
            EventCaller eventItem = new EventCaller { Length = 0, MessageCounter = item, MessageType = 1, OffsetPosition = 4 * 4 };

            _eventsToSendQueue.Enqueue(eventItem);
        }

        public void RaiseEvent(int request)
        {
            AddToQueue(request);
        }
        public void SubscribeToEvent(Action request)
        {


        }

        private void EventspinnerOut()
        {
            using (var viewAccessor = GetOutAccessor())
            {
                bool ackd = true;

                viewAccessor.Write(sizeof(Int32) * 3, true);
                while (this._eventSpinnerThreadsRunning)
                {
                    //check if anything in outgoing queue
                    //check if last message was processed
                    //if processed write new item to queue

                    if (ackd)
                    {
                        EventCaller q;
                        if (!_eventsToSendQueue.TryDequeue(out q)) continue;
                        viewAccessor.Write(0, ref q);
                        ackd = false;
                    }
                    else
                    {
                        viewAccessor.Read(0, out ackd);

                        if (ackd) continue;
                        Thread.Yield(); //Release thread slice so that we don't consume 100% of a core
                    }
                }
            }
        }
        private void EventspinnerIn()
        {
            using (var viewAccessor = GetInAccessor())
            {
                while (_eventSpinnerThreadsRunning)
                {
                    //check if anything in outgoing queue
                    //check if last message was processed
                    //if processed write new item to queue

                    EventCaller q;

                    viewAccessor.Read(0, out q);
                    if (q.ACK)
                    {
                        Thread.Yield();
                        continue;
                    }

                    processEventDelegate(q);

                    //Write ack
                    viewAccessor.Write(0, true);
                }
            }
        }

        private void DebugProcessIncomingEvent(EventCaller q)
        {

            Console.WriteLine(q.MessageCounter);
        }

       private void StartEventspinners()
        {
            _eventSpinnerThreadIn = new Thread(EventspinnerIn);
            _eventSpinnerThreadIn.Start();
            _eventSpinnerThreadOut = new Thread(EventspinnerOut);
            _eventSpinnerThreadOut.Start();
        }

        private struct EventCaller
        {
            public Boolean ACK;
            public int MessageCounter;
            public int MessageType;
            public int OffsetPosition;
            public int Length;
        }


        public Version Version()
        {
            return new Version(  MethodInfo.GetCurrentMethod().DeclaringType.Assembly.ImageRuntimeVersion.TrimStart('v'));
        }




        public void Dispose()
        {
            _eventSpinnerThreadsRunning = false;

            _eventSpinnerThreadIn.Join(500);
            _eventSpinnerThreadOut.Join(500);

            if (_memoryMappedIn != null)
            {
                _memoryMappedIn.Dispose();
            }

            if (_memoryMappedOut != null)
            {
                _memoryMappedOut.Dispose();
            }
        }
    }
}
