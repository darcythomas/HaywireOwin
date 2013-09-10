using System;

namespace MemoryMapBridgeProxy
{
    public class AssignProcessors
    {
        private HaywireBridge _haywireBridge;
        public Action<EventCaller> ProcessEventDelegate;

        public AssignProcessors(HaywireBridge haywireBridge)
        {
            _haywireBridge = haywireBridge;
        }

        public void SetDefaultIncomingEventProcessor(HaywireStartUpMode mode)
        {

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

        private void PerformanceTestProcessIncomingEvent(EventCaller eventObject)
        {
            //decide what to do based on message type

            if (eventObject.MessageType % 2 == 0)
            {
                //is a response; ignore
                if (eventObject.MessageType != 0)
                {
                    _haywireBridge._memoryManagmentFactory.ReturnAllocation(new MemoryAllocation(eventObject));

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
                        _haywireBridge._processorViewAccessor.ReadArray(eventObject.OffsetPosition, data, 0, eventObject.MessageLength);
                        _haywireBridge._memoryManagmentFactory.ReturnAllocation(new MemoryAllocation(eventObject));

                        String message = Helpers.GetString(data);
                        Console.WriteLine(message);

                        _haywireBridge.RaiseEvent(message, MessageType.EchoReply);
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

        private void DebugProcessIncomingEvent(EventCaller q)
        {
            Console.WriteLine(q.MessageCounter);
        }
    }
}