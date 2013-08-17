using System;

namespace HaywireOwinTechSpike
{
    public interface IThreadRunner: IDisposable
    {
        void Init();
        void Run();
    }
}