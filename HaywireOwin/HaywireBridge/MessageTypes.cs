using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryMapBridgeProxy
{

/// <summary>
/// Message types.
/// Replies are ALWAYS even numbers (requests Always odd)
/// </summary>
   public enum MessageType
    {
       Default = 0,
       HeartBeat = 1,
       HeartBeatReply = 2,
       Echo = 3,
       EchoReply = 4
    }
}
