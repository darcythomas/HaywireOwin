using System;

namespace MemoryMapBridgeProxy
{
    public interface IEventSpinners : IDisposable
    {
        void StartEventspinners();
    }
}