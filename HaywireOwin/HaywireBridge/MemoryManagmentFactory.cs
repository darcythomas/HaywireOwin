using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace MemoryMapBridgeProxy
{
    public class MemoryManagmentFactory : IMemoryManagmentFactory
    {
        ConcurrentQueue<MemoryAllocation> _allocationPool = new ConcurrentQueue<MemoryAllocation>();
        ConcurrentQueue<MemoryAllocation> _inputBuffer = new ConcurrentQueue<MemoryAllocation>();
        private int numberOfBlockInPool;
        private int splitRatio = 3;
        private int averageSize;
        private int standardDeviation;

        public MemoryManagmentFactory(int maxSize, int reservedBlock)
        {
            //populate pool
        }
        public MemoryAllocation GetAllocation(int minSize)
        {
            //put each allocation into a collection in a dictionary of sizes
            //i.e., set of boxes each with a collection of lager and larger allocations
            // if you want a allocation you grab it from the smallest pool that is larger than your request

            //if there is nothing in that pool then ask for the next size up (recursively)
            // or it could just retry
            //record that you did that you did
            
            throw new NotImplementedException();
        }

        public void ReturnAllocation(MemoryAllocation finishedWith)
        {
            //put back into pool
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
    }
}
