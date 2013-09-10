using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryMapBridgeProxy
{
    public interface IHaywireBridge : IDisposable
    {
        String FileName { get; }
        [Obsolete]
        void RaiseEvent(int request);//TODO remove

        void RaiseEvent(String request, MessageType messageType);//TODO look at better overloads 
        void SubscribeToEvent(Action<EventCaller, MemoryMappedViewAccessor> request, MessageType messageType);
        Version Version { get; }
    }
}
