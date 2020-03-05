using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch
{
    internal interface IArchScheduler: IDisposable
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
    internal sealed class SimpleAchScheduler : IArchScheduler, IDisposable {

        private object _locker = new object();
        private int _requestCount = 0;
        private bool _stop = false;
        //private object _schedulingLocker = new object();
        //private bool _isScheduling = false;

        private ConcurrentQueue<ThreadWrapper> _threadPool = new ConcurrentQueue<ThreadWrapper>();
        //We have to use queue to avoid cases cyclic performing rescheduled item!
        private Queue<Action> _queue = new Queue<Action>();

        public SimpleAchScheduler(int threadsCount = 4, CancellationToken cancellationToken = default)
        {
            InitThreadPool(threadsCount, cancellationToken);
            BeginSchedulling(cancellationToken);
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

        private void InitThreadPool(int initialCount, CancellationToken cancellationToken) {
            for (int idx = 0; idx < initialCount; idx++) {
                _threadPool.Enqueue(new ThreadWrapper(cancellationToken));
            }
        }

        private void BeginSchedulling(CancellationToken cancellationToken)
        {
            var schedulingThread = new Thread(() => {
                while(true) {
                    if (cancellationToken.IsCancellationRequested) {
                        foreach(var thread in _threadPool) {
                            _queue.Clear();
                            thread.Pulse();
                        }
                        break;
                    }
                    if(_stop) break;

                    if (_threadPool.Count > 0) {
                        if(TryDequeueNextWorkItem(out Action nextItem)) {
                            Interlocked.Increment(ref _requestCount);
                            if(_threadPool.TryDequeue(out ThreadWrapper thread)) {
                                //Console.Write($"\r Queue length: {_queue.Count}");
                                Action nextItemWrapper = () => {
                                    try {
                                        nextItem();
                                    }
                                    finally {
                                        Interlocked.Decrement(ref _requestCount);
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
                //Console.WriteLine("Scheduling thread terminated");
            });
            schedulingThread.Start();
        }

        private void StopScheduling() {
            while (true) {
                if (_requestCount > 0) continue;
                break;
            }
            foreach (var thread in _threadPool) {
                thread.Dispose();
            }
            _stop = true;
            _threadPool.Clear();
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

        public void Dispose() {
            StopScheduling();
        }

        private sealed class ThreadWrapper: IDisposable {

            private bool _disposed = false;
            private Thread _targetThread;
            private AutoResetEvent _workAwaiter = new AutoResetEvent(initialState: false);
            private Action _workItem;

            public ThreadWrapper(CancellationToken cancellationToken)
            {
                _targetThread = new Thread(() => {
                    while (true) {
                        _workAwaiter.WaitOne();

                        if (_disposed) break;

                        // break the execution
                        if (cancellationToken.IsCancellationRequested) break;

                        _workItem();
                    }
                    //Console.WriteLine($"ThreadId: {Thread.CurrentThread.ManagedThreadId} terminated.");
                });
                _targetThread.Start();
            }

            public void Start(Action workItem) {
                _workItem = workItem;
                _workAwaiter.Set();
            }

            public void Dispose() {
                _disposed = true;
                Pulse();
                _targetThread.Join();
            }

            internal void Pulse()
            {
                _workItem = () => { };
                _workAwaiter.Set();
            }
        }
    }
}