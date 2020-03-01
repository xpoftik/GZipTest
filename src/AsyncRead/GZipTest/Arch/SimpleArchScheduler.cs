using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch
{
    internal interface IArchScheduler
    {
        WaitHandle ScheduleWorkItem<T>(Func<T> workItem, Action<T> callback);
        WaitHandle ScheduleWorkItem(Action workItem, Action callback);
    }

    internal sealed class SimpleAchScheduler : IArchScheduler {

        private object _locker = new object();

        private ConcurrentQueue<ThreadWrapper> _threads = new ConcurrentQueue<ThreadWrapper>();
        //We have to you queue to avoid cases cyclic performing rescheduled item!
        private Queue<Action> _queue = new Queue<Action>();

        public SimpleAchScheduler(int threadCount = 4)
        {
            InitThreadsPool(threadCount);
            BeginSchedulling();
        }

        //TODO: ЧТО ДЕЛАТЬ С ЭКСЕПШЕНАМИ?
        public WaitHandle ScheduleWorkItem<T>(Func<T> workItem, Action<T> callback)
        {
            ThrowIf.Argument.IsNull(workItem, nameof(workItem));

            var waitHandle = new ManualResetEvent(initialState: false);
            Action threadStart = () => {
                try{
                    var result = workItem();
                    //TODO: we could schedule invocation of callback via scheduler.
                    callback?.Invoke(result);
                } finally {
                    waitHandle.Set();
                }
            };
            SchedulePreparedWorkItem(threadStart);

            return waitHandle;
        }

        public WaitHandle ScheduleWorkItem(Action workItem, Action callback)
        {
            ThrowIf.Argument.IsNull(workItem, nameof(workItem));

            var waitHandle = new ManualResetEvent(initialState: false);
            Action threadStart = () => {
                try{
                    workItem();
                    //TODO: we could schedule invocation of callback via scheduler.
                    callback?.Invoke();
                } finally {
                    waitHandle.Set();
                }
            };
            SchedulePreparedWorkItem(threadStart);

            return waitHandle;
        }

        //public void RescheduleWorkItem(Action workItem, Action callback, WaitHandle waitHandle) { 
            
        //}

        private void SchedulePreparedWorkItem(Action threadStart)
        {
            lock (_locker) {
                _queue.Enqueue(threadStart);
            }
        }

        private void InitThreadsPool(int initialCount) {
            for (int idx = 0; idx < initialCount; idx++) {
                _threads.Enqueue(new ThreadWrapper());
            }
        }

        //TODO: Transform it to StateMachine
        private void BeginSchedulling()
        {
            var schedulingThread = new Thread(() => {
                //TODO: Do we have to stop this thread somehow?
                while(true) {
                    if (_threads.Count > 0) {
                        if(TryDequeueNextWorkItem(out Action nextItem)){
                            if(_threads.TryDequeue(out ThreadWrapper thread)) {
                                //Console.WriteLine($"Queue length: {_queue.Count}");
                                Action nextItemWrapper = () => {
                                    try {
                                        nextItem();
                                    }
                                    finally {
                                        _threads.Enqueue(thread);
                                    }
                                };
                                thread.Start(nextItemWrapper);
                            } else {
                                SchedulePreparedWorkItem(nextItem);
                            }
                            //var thread = _threads.Dequeue();
                            //if(thread != null) {
                            //    Action nextItemWrapper = () => {
                            //        try {
                            //            nextItem();
                            //        }
                            //        finally {
                            //            _threads.Enqueue(thread);
                            //        }
                            //    };
                            //    thread.Start(nextItemWrapper);
                            //} else {
                            //    SchedulePreparedWorkItem(nextItem);
                            //}
                        }
                    }
                }
            });
            schedulingThread.Start();
        }

        private bool TryDequeueNextWorkItem(out Action action)
        {
            bool lockTaken = false;
            action = null;
            Monitor.TryEnter(_locker, ref lockTaken);
            if (lockTaken) {
                try {
                    if (_queue.Count > 0) {
                        action = _queue.Dequeue();
                    }
                } finally {
                    Monitor.Exit(_locker);
                }
            }
            return action != null;
        }

        private sealed class ThreadWrapper {

            private Thread _targetThread;
            private AutoResetEvent _workAwaiter = new AutoResetEvent(initialState: false);
            private Action _workItem;

            public ThreadWrapper()
            {
                _targetThread = new Thread(() => {
                    while (true) {
                        _workAwaiter.WaitOne();

                        _workItem();
                    }
                });
                _targetThread.Start();
            }

            public void Start(Action workItem) {
                _workItem = workItem;
                _workAwaiter.Set();
            }
        }
    }
}