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
        public static readonly string DefaultMemorymappedFileName = "HaywireBridgeMemoryMap";
        private static readonly Mutex MemoryMapsetupMutex = new Mutex(false, SystemWideMutexName);
        private ConcurrentQueue<Object> _eventsRecievedQueue = new ConcurrentQueue<Object>();

        public Action<EventCaller> ProcessEventDelegate;
        public Dictionary<MessageType, Action<EventCaller, MemoryMappedViewAccessor>> ProcessEventDelegates = new Dictionary<MessageType, Action<EventCaller, MemoryMappedViewAccessor>>();

        public Action<String, MessageType> AddEventToQueueDelegate;
        public String FileName { get; private set; }
        internal MemoryMappedFile MemoryMappedIn;
        internal MemoryMappedFile MemoryMappedOut;
        internal MemoryMappedFile ProcessorMappedFile;
        private const int BridgeSize = 1024 * 8;
        public MemoryMappedViewAccessor _processorViewAccessor;
        private MemoryMappedViewStream _processorViewStream;

        //TODO rename parameters
        public void SubscribeToEvent(Action<EventCaller, MemoryMappedViewAccessor> request, MessageType messageType)
        {
            if (ProcessEventDelegates.ContainsKey(messageType))
            {
                ProcessEventDelegates[messageType] = request;
            }
            else
            {
                ProcessEventDelegates.Add(messageType, request);
            }
        }

        public Version Version { get { return MethodBase.GetCurrentMethod().DeclaringType.Assembly.GetName().Version; } }

        public EventLoaderUnloader EventLoaderUnloader
        {
            get { return _eventLoaderUnloader; }
        }

        public MemoryManagmentFactory _memoryManagmentFactory;
        private readonly EventSpinners _eventSpinners;
        private readonly EventLoaderUnloader _eventLoaderUnloader;


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
            _eventLoaderUnloader = new EventLoaderUnloader(this);
            _eventSpinners = new EventSpinners(this);
            SetupAddEventToQueueDelegate();

        }

        private void SetupAddEventToQueueDelegate()
        {
            AddEventToQueueDelegate = _eventLoaderUnloader.AddToQueue;
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
            FileName = String.IsNullOrWhiteSpace(fileName) ? DefaultMemorymappedFileName : fileName;
        }

        private void SetupMemoryMappedFiles(MemoryMappedFileSecurity customSecurity)
        {
            //TODO: look at just using a mutex to setup (remove try catch)
            MemoryMapsetupMutex.WaitOne();
            try
            {
                MemoryMappedIn = MemoryMappedFile.CreateNew(FileName + "-MasterInSlaveOut", BridgeSize,
                    MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, customSecurity,
                    HandleInheritability.Inheritable);
            }
            catch (Exception)
            {
                MemoryMappedIn = MemoryMappedFile.CreateOrOpen(FileName + "-SlaveInMasterOut", BridgeSize,
                    MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, customSecurity,
                    HandleInheritability.Inheritable);
            }

            try
            {
                MemoryMappedOut = MemoryMappedFile.CreateNew(FileName + "-SlaveInMasterOut", BridgeSize,
                    MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, customSecurity,
                    HandleInheritability.Inheritable);
            }
            catch (Exception)
            {
                MemoryMappedOut = MemoryMappedFile.CreateOrOpen(FileName + "-MasterInSlaveOut", BridgeSize,
                    MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, customSecurity,
                    HandleInheritability.Inheritable);
            }


            //TODO this tempory replace later Or make sure it is disposed properly
            ProcessorMappedFile = MemoryMappedFile.CreateOrOpen(FileName + "-Processor", BridgeSize,
                   MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, customSecurity,
                   HandleInheritability.Inheritable);


            _processorViewAccessor = ProcessorMappedFile.CreateViewAccessor();
            //  _processorViewStream = _processorMappedFile.CreateViewStream(BridgeSize,BridgeSize);
            _memoryManagmentFactory = new MemoryManagmentFactory(BridgeSize);


            MemoryMapsetupMutex.ReleaseMutex();
        }




        private void SetDefaultIncomingEventProcessor(HaywireStartUpMode mode)
        {

            ProcessEventDelegate = SubscribedProcessIncomingEvent;
            ProcessEventDelegates.Add(MessageType.Default, ProcessIncomingEventDoNothing);
            return;


            //TODO Change this to use MEF to load processors

            switch (mode)
            {
                case HaywireStartUpMode.Default:
#if (DEBUG)
                    //If a debug release then use the debug processor, otherwise fall through and use the normal one
                    ProcessEventDelegate = DebugProcessIncomingEvent;
                    break;
#endif
                case HaywireStartUpMode.Normal:
                    ProcessEventDelegate = NormalProcessIncomingEvent;
                    break;
                case HaywireStartUpMode.Debug:
                    ProcessEventDelegate = DebugProcessIncomingEvent;
                    break;
                case HaywireStartUpMode.PerformanceTest:
                    ProcessEventDelegate = PerformanceTestProcessIncomingEvent;
                    break;
            }

        }

        private void NormalProcessIncomingEvent(EventCaller obj)
        {
            throw new NotImplementedException();
        }

        private void ProcessIncomingEventDoNothing(EventCaller eventCaller, MemoryMappedViewAccessor accessor)
        {
            //Do Nothing
        }
        private void SubscribedProcessIncomingEvent(EventCaller obj)
        {
            ProcessEventDelegates[(MessageType)obj.MessageType].Invoke(obj, _processorViewAccessor);
        }

        private void PerformanceTestProcessIncomingEvent(EventCaller eventObject)
        {
            //decide what to do based on message type

            if (eventObject.MessageType % 2 == 0)
            {
                //is a response; ignore
                if (eventObject.MessageType != 0)
                {
                    _memoryManagmentFactory.ReturnAllocation(new MemoryAllocation(eventObject));

                }

                //TODO memory pool release 
            }
            else
            {
                MessageType messageType = (MessageType)eventObject.MessageType;
                switch (messageType)
                {
                    case MessageType.Echo:
                        Byte[] data = new byte[eventObject.MessageLength];
                        _processorViewAccessor.ReadArray(eventObject.OffsetPosition, data, 0, eventObject.MessageLength);
                        _memoryManagmentFactory.ReturnAllocation(new MemoryAllocation(eventObject));

                        String message = data.GetString();
                        Console.WriteLine(message);

                        RaiseEvent(message, MessageType.EchoReply);
                        break;


                    case MessageType.Default:
                    default:
                        //ignore

                        break;
                }
            }


            //get the string from memory
            //Convert bytes to string


            //respond back
        }

        public MemoryMappedViewAccessor GetInAccessor()
        {
            return MemoryMappedIn.CreateViewAccessor();
        }
        public MemoryMappedViewAccessor GetOutAccessor()
        {
            return MemoryMappedOut.CreateViewAccessor();
        }

        //private void AddToQueue(int item)
        //{
        //    EventCaller eventItem = new EventCaller { MessageLength = 0, MessageCounter = item, MessageType = 1, OffsetPosition = 4 * 4 };

        //    EventsToSendQueue.Enqueue(eventItem);
        //}




        [Obsolete]
        public void RaiseEvent(int request)
        {
            RaiseEvent(request.ToString("F0"), MessageType.Echo);
        }

        public void RaiseEvent(String request, MessageType messagetype)
        {
            AddEventToQueueDelegate(request, messagetype);
        }



        private void DebugProcessIncomingEvent(EventCaller q)
        {
            Console.WriteLine(q.MessageCounter);
        }




        public void Dispose()
        {
            _eventSpinners.Dispose();

            if (MemoryMappedIn != null)
            {
                MemoryMappedIn.Dispose();
            }

            if (MemoryMappedOut != null)
            {
                MemoryMappedOut.Dispose();
            }
        }
    }
}
