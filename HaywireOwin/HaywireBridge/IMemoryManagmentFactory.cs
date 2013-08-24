namespace MemoryMapBridgeProxy
{
    public interface IMemoryManagmentFactory
    {
        MemoryAllocation GetAllocation(int minSize);
        void ReturnAllocation(MemoryAllocation finishedWith);
    }
}