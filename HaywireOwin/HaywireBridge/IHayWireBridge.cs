using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryMapBridgeProxy
{
    public interface IHaywireBridge : IDisposable
    {
        String FileName { get; }
        void RaiseEvent(int request);//TODO remove
        void RaiseEvent(String request, MessageType messageType);
        void SubscribeToEvent(Action request);
        Version Version { get; }
    }
}
