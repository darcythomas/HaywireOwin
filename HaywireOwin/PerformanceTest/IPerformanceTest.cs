using MemoryMapBridgeProxy;

namespace TestPerformance
{
    public interface IPerformanceTest
    {
        TestResult RunTest(IHaywireBridge sender);
    }
}