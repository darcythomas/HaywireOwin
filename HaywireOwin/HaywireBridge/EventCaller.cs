using System;
using System.Collections.Generic;
using System.Linq;


namespace MemoryMapBridgeProxy
{
    public struct EventCaller
    {
        // ReSharper disable once UnassignedField.Compiler
        public Boolean ACK;
        public int MessageCounter;//may not need this
        public int MessageType; //TODO define Messages types in a enum
        public int OffsetPosition;
        public int MessageLength;
        public int MemoryAllocationLength;
    }
}
