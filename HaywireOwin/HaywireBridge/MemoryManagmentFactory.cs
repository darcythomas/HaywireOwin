using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryMapBridgeProxy
{
    public class MemoryManagmentFactory : IMemoryManagmentFactory
    {
        private static ConcurrentQueue<MemoryAllocation> _allocationPool = new ConcurrentQueue<MemoryAllocation>();
        ConcurrentQueue<MemoryAllocation> _inputBuffer = new ConcurrentQueue<MemoryAllocation>();
        private int numberOfBlockInPool;
        private int splitRatio = 3;
        private double averageSize;
        private int standardDeviation;

        public MemoryManagmentFactory(int maxSize, int reservedBlockOffset = 0)
        {
            //populate pool

            //TODO this is just a quick and dirty way of allocating blocks in a pool. Fix later
            int maxItemsize = Convert.ToInt32(maxSize / 4);
            int currentOffset = reservedBlockOffset;

            Random rand = new Random();
            while (maxSize > 129)
            {
                int length = maxSize;// rand.Next(128, maxItemsize);
                MemoryAllocation allocation;

                allocation.Offset = currentOffset;
                allocation.Lenght = length;

                currentOffset += length;

                allocation.NextBlock = currentOffset;

                maxSize -= length;
                numberOfBlockInPool++;
                _allocationPool.Enqueue(allocation);
            }

            IEnumerable<MemoryAllocation> itemsInPool = _allocationPool.ToArray();

            averageSize = itemsInPool.Average(a => a.Lenght);
            Debug.Write("Average Size item in the pool " + averageSize);

        }

        // prevent this constructor from being called
        private MemoryManagmentFactory() { }


        public MemoryAllocation GetAllocation(int minSize)
        {
            //put each allocation into a collection in a dictionary of sizes
            //i.e., set of boxes each with a collection of lager and larger allocations
            // if you want a allocation you grab it from the smallest pool that is larger than your request

            //if there is nothing in that pool then ask for the next size up (recursively)
            // or it could just retry
            //record that you did that you did

            MemoryAllocation dequeuedItem;
            while (!_allocationPool.TryDequeue(out dequeuedItem) && dequeuedItem.Lenght >= minSize)
            {
                if (dequeuedItem.Lenght > 0)
                {
                    ReturnAllocation(dequeuedItem);
                }
                Thread.Yield();
            }
            return dequeuedItem;
        }

        public void ReturnAllocation(MemoryAllocation finishedWith)
        {
            //put back into pool
            _allocationPool.Enqueue(finishedWith);
            return;
            
            int count = _inputBuffer.Count();
            if (count <= numberOfBlockInPool / 10) return;

            //Try to keep in order
            List<MemoryAllocation> itemsToReAdd = new List<MemoryAllocation>();
            for (int i = 0; i < count; i++)
            {
                MemoryAllocation dequeuedItem;
                if (_inputBuffer.TryDequeue(out dequeuedItem))
                {
                    itemsToReAdd.Add(dequeuedItem);
                }
            }
            foreach (var memoryAllocation in itemsToReAdd.OrderBy(o => o.Offset))
            {
                _allocationPool.Enqueue(memoryAllocation);
            }
        }

        private void Inspector()
        {
            //run in separate thread
            // Check if one pool is getting starved if it is then break or add to get what you want 
        }
    }

    public struct MemoryAllocation
    {
        public int Offset;
        public int Lenght;
        public int NextBlock;

        public MemoryAllocation(EventCaller eventObject)
        {
            Offset = eventObject.OffsetPosition;
            Lenght = eventObject.MemoryAllocationLength;
            NextBlock = Offset + Lenght;
        }
    }
}
