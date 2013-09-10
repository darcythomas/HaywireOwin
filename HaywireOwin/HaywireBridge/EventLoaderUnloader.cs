using System;
using System.Collections.Concurrent;

namespace MemoryMapBridgeProxy
{
    public class EventLoaderUnloader
    {
        private readonly HaywireBridge _haywireBridge;
        public readonly ConcurrentQueue<EventCaller> EventsToSendQueue = new ConcurrentQueue<EventCaller>();

        public EventLoaderUnloader(HaywireBridge haywireBridge)
        {
            _haywireBridge = haywireBridge;
        }

        //TODO this method currently is not queuing messages properly 
        public void AddToQueue(String item, MessageType messageType)
        {


            byte[] messageBytes = item.GetBytes();
            int length = messageBytes.Length;

            EventCaller eventItem = GetEventAllocation(messageType, length);

            _haywireBridge._processorViewAccessor.WriteArray(eventItem.OffsetPosition, messageBytes, 0, messageBytes.Length);

            EventsToSendQueue.Enqueue(eventItem);
        }

        public EventCaller GetEventAllocation(MessageType messageType, int length)
        {
            MemoryAllocation allocation = _haywireBridge._memoryManagmentFactory.GetAllocation(length);
            return new EventCaller { MessageLength = length, MessageType = (int)messageType, OffsetPosition = allocation.Offset, MemoryAllocationLength = allocation.Lenght };
        }
    }
}