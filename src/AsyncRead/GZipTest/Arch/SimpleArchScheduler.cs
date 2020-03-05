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
        WaitHandle ScheduleWorkItem(Action workItem, Action callback = null);
    }

    /// <summary>
    /// Simple scheduler.
    /// Just has thread pool and tasks queue. 
    /// It dequeues tasks from queue one by one and starts on one of available thread.
    /// 
    /// </summary>
    internal sealed class SimpleAchScheduler : IArchScheduler {

        private object _locker = new object();
        //private object _schedulingLocker = new object();
        //private bool _isScheduling = false;

        private ConcurrentQueue<ThreadWrapper> _threadPool = new ConcurrentQueue<ThreadWrapper>();
        //We have to use queue to avoid cases cyclic performing rescheduled item!
        private Queue<Action> _queue = new Queue<Action>();

        public SimpleAchScheduler(int threadsCount = 4)
        {
            InitThreadPool(threadsCount);
            BeginSchedulling();
        }

        public WaitHandle ScheduleWorkItem<T>(Func<T> workItem, Action<T> callback)
        {
            ThrowIf.Argument.IsNull(workItem, nameof(workItem));

            var waitHandle = new ManualResetEvent(initialState: false);
            Action threadStart = () => {
                try{
                    var result = workItem();
                    //We could schedule invocation of callback via scheduler.
                    callback?.Invoke(result);
                } finally {
                    waitHandle.Set();
                }
            };
            SchedulePreparedWorkItem(threadStart);

            return waitHandle;
        }

        public WaitHandle ScheduleWorkItem(Action workItem, Action callback = null)
        {
            ThrowIf.Argument.IsNull(workItem, nameof(workItem));

            var waitHandle = new ManualResetEvent(initialState: false);
            Action threadStart = () => {
                try{
                    workItem();
                    //We could schedule invocation of callback via scheduler.
                    callback?.Invoke();
                } finally {
                    waitHandle.Set();
                }
            };
            SchedulePreparedWorkItem(threadStart);

            return waitHandle;
        }

        private void SchedulePreparedWorkItem(Action threadStart)
        {
            lock (_locker) {
                _queue.Enqueue(threadStart);
            }
        }

        private void InitThreadPool(int initialCount) {
            for (int idx = 0; idx < initialCount; idx++) {
                _threadPool.Enqueue(new ThreadWrapper());
            }
        }

        private void BeginSchedulling()
        {
            var schedulingThread = new Thread(() => {
                while(true) {
                    if (_threadPool.Count > 0) {
                        if(TryDequeueNextWorkItem(out Action nextItem)) {
                            if(_threadPool.TryDequeue(out ThreadWrapper thread)) {
                                //Console.Write($"\r Queue length: {_queue.Count}");
                                Action nextItemWrapper = () => {
                                    try {
                                        nextItem();
                                    }
                                    finally {
                                        _threadPool.Enqueue(thread);
                                    }
                                };
                                thread.Start(nextItemWrapper);
                            } else {
                                SchedulePreparedWorkItem(nextItem);
                            }
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