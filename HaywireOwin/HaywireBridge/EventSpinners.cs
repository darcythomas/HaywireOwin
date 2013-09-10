using System;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryMapBridgeProxy
{
   

    public class EventSpinners: IEventSpinners
    {
        private readonly HaywireBridge _haywireBridge;
        private Thread _eventSpinnerThreadIn;
        private Thread _eventSpinnerThreadOut;
        private Boolean _eventSpinnerThreadsRunning = true;

        public EventSpinners(HaywireBridge haywireBridge)
        {
            _haywireBridge = haywireBridge;
            StartEventspinners();
            
        }

        private void EventspinnerOut()
        {
            using (var viewAccessor = _haywireBridge.GetOutAccessor())
            {
                bool ackd = true;

                viewAccessor.Write(sizeof(Int32) * 3, true);
                while (_eventSpinnerThreadsRunning)
                {
                    //check if anything in outgoing queue
                    //check if last message was processed
                    //if processed write new item to queue


                    //HINT: try writing to a set of slots not just one, may be more efficient with thread time slicing
                    if (ackd)
                    {
                        EventCaller q;
                        if (!_haywireBridge.EventLoaderUnloader.EventsToSendQueue.TryDequeue(out q)) continue;
                        viewAccessor.Write(0, ref q);
                        ackd = false;
                    }
                    else
                    {
                        viewAccessor.Read(0, out ackd);

                        if (ackd) continue;
                        Thread.Yield(); //Release thread slice so that we don't consume 100% of a core
                    }
                }
            }
        }

        private void EventspinnerIn()
        {
            using (var viewAccessor = _haywireBridge.GetInAccessor())
            {
                while (_eventSpinnerThreadsRunning)
                {
                    //check if anything in outgoing queue
                    //check if last message was processed
                    //if processed write new item to queue

                    EventCaller eventItem;

                    viewAccessor.Read(0, out eventItem);
                    if (eventItem.ACK)
                    {
                        Thread.Yield();
                        continue;
                    }

                    
                    Task.Factory.StartNew(() => _haywireBridge.ProcessEventDelegate(eventItem));
                   
                    //Write ack
                    viewAccessor.Write(0, true);
                }
            }
        }

        public void StartEventspinners()
        {
            _eventSpinnerThreadIn = new Thread(EventspinnerIn);
            _eventSpinnerThreadIn.Start();
            _eventSpinnerThreadOut = new Thread(EventspinnerOut);
            _eventSpinnerThreadOut.Start();
        }

        public virtual void Dispose()
        {
            _eventSpinnerThreadsRunning = false;

            _eventSpinnerThreadIn.Join(500);
            _eventSpinnerThreadOut.Join(500);
        }

    }
}