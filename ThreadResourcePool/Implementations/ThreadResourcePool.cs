using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadResourcePool
{
    public class ThreadResourcePool : IThreadResourcePool
    {
        private readonly ConcurrentDictionary<string, Resource> _resources = new ConcurrentDictionary<string, Resource>();
        private readonly BlockingCollection<WorkItem> _pendingWorkItems = new BlockingCollection<WorkItem>(new ConcurrentQueue<WorkItem>());
        private readonly List<WorkItem> _waitingWorkItems = new List<WorkItem>();
        private readonly Thread _dispatcher;

        public ThreadResourcePool(CancellationToken cancellationToken)
        {
            _dispatcher = new Thread(Dispatcher);
            _dispatcher.Start(cancellationToken);
        }

        private void Dispatcher(object obj)
        {
            CancellationToken token = (CancellationToken)obj;

            while (!token.IsCancellationRequested) // Schedule from the waiting list first
            {
                for (int i = 0; i < _waitingWorkItems.Count; i++)
                {
                    if (Determine(_waitingWorkItems[i]))
                    {
                        //Console.WriteLine($"{_waitingWorkItems[i].State} has been scheduled and being removed from the waiting list");
                        _waitingWorkItems.Remove(_waitingWorkItems[i]); // If scheduled, then remove, otherwise just keep
                        continue;
                    }
                    else
                    {
                        continue;
                        //Console.WriteLine($"{_waitingWorkItems[i].State} was kept in the waiting list");
                    }
                }

                #region strict liner dispatch e.g. 12345
                //if (_waitingWorkItems.Count > 0)
                //{
                //    if (Determine(_waitingWorkItems[0]))
                //    {
                //        _waitingWorkItems.Remove(_waitingWorkItems[0]);
                //    }
                //}

                //if (_waitingWorkItems.Count > 0)
                //{
                //    continue;
                //}
                #endregion

                WorkItem workitem = null;
                if (_pendingWorkItems.TryTake(out workitem, 0, token)) //Try Dequeue
                {
                    if (!Determine(workitem))//Try schedule at once
                    {
                        _waitingWorkItems.Add(workitem);// If can't be scheduled, save to waiting list
                        //Console.WriteLine($"{workitem.State} has been dequeued but was kept in the waiting list");
                    }
                    else
                    {
                        //Console.WriteLine($"{workitem.State} has been dequeued and scheduled");
                    }
                }
            }

            _pendingWorkItems.CompleteAdding();
            _pendingWorkItems.Dispose();
            _waitingWorkItems.Clear();
            _resources.Clear();
        }

        private bool Determine(WorkItem work)
        {
            Resource acquiring = null;
            foreach (var resrouce in work.Request) //Verify all requested resources
            {
                _resources.TryGetValue(resrouce, out acquiring); //Try get by resource name

                if (acquiring == null) //Resource does not exist at this moment
                {
                    //Console.WriteLine($"{resrouce} of {work.State} does not exist at this moment");
                    return false; //Save to waiting list
                }

                if (acquiring.IsOccpuied) //This resouce is engaged
                {
                    //Console.WriteLine($"{resrouce} of {work.State} is engaged at this moment");
                    return false; //Save to waiting list
                }
            }

            foreach (var resrouce in work.Request) //Acquire all resources
            {
                _resources[resrouce].IsOccpuied = true;
                //Console.WriteLine($"{resrouce} of {work.State} is being acquired");
            }

            Task.Factory.StartNew(() =>
            {
                try
                {
                    //Console.WriteLine($"invoking {work.State}");
                    work.CallBack(work.State);
                    //Console.WriteLine($"{work.State} invoked");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            })
            .ContinueWith(cb =>
            {
                foreach (var resrouce in work.Request)
                {
                    _resources[resrouce].IsOccpuied = false; //Release resource
                    //Console.WriteLine($"{resrouce} of {work.State} is being released");
                }
            }); // Schedule task

            return true; // Scheduled
        }

        public bool AddResource(string name)
        {
            return _resources.TryAdd(name, new Resource(name));
        }

        public void QueueUserWorkItem(WaitCallback callBack, object state, List<string> request)
        {
            List<string> snapshot = new List<string>();
            snapshot.AddRange(request);

            WorkItem work = new WorkItem
            {
                CallBack = callBack,
                State = state,
                Request = snapshot.AsReadOnly()
            };
            _pendingWorkItems.Add(work); //Return at once
        }

        private class WorkItem
        {
            public WaitCallback CallBack { get; set; }
            public object State { get; set; }
            public ReadOnlyCollection<string> Request { get; set; }
        }

        private class Resource
        {
            public string Name { get; private set; }

            public volatile bool IsOccpuied;

            public Resource(string name)
            {
                Name = name;
            }
        }
    }
}
