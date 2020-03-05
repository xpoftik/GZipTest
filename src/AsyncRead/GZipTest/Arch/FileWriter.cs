using GZipTest.Arch.Abstract;
using GZipTest.Arch.Model;
using GZipTest.Arch.Results;
using GZipTest.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace GZipTest.Arch
{
    /// <summary>
    /// Async file wrapper.
    /// 
    /// </summary>
    internal sealed class FileWriter: DisposableObject, IWriter<Block>, IDisposable
    {
        private enum States { 
            Start,
            Success,
            Error,
            Finalize
        }

        private object _locker = new object();
        private readonly IArchScheduler _scheduler;

        private readonly string _target;
        private Stack<Stream> _handlersPool = new Stack<Stream>();
        private List<WaitHandle> _awaiters = new List<WaitHandle>();

        public FileWriter(string target, IArchScheduler scheduler)
        {
            ThrowIf.Argument.StringIsEmpty(target, nameof(target));
            ThrowIf.Argument.IsNull(scheduler, nameof(scheduler));

            File.Create(target).Close();

            _target = target;
            _scheduler = scheduler;
        }

        public WaitHandle WriteAsync(Block block, long offset, Action<IAsyncResult<Block>> callback, CancellationToken cancellationToken)
        {
            ThrowIf.Argument.IsNull(block, nameof(block));
            ThrowIf.Argument.LessThanZero(offset, nameof(offset));
            ThrowIf.Argument.IsNull(callback, nameof(callback));

            if (block.Index == -1) {
                var wh = new ManualResetEvent(initialState: true);
                callback(new ValueResult<Block>(block, wh));
                
                return wh;
            }

            var waitHandle = new ManualResetEvent(initialState: false);
            Action<States, CancellationToken> writerStateMachine = null;
            Exception exception = null;
            writerStateMachine = (state, token) => {
                if (token.IsCancellationRequested) {
                    waitHandle.Set();
                    return;
                }

                switch (state) {
                    case States.Start:
                        try {
                            WriteBlock(block.Payload, offset, block.Size);
                            _scheduler.ScheduleWorkItem(() => writerStateMachine(States.Success, token));
                        } catch (Exception ex) {
                            exception = ex;
                            _scheduler.ScheduleWorkItem(() => writerStateMachine(States.Error, token));
                        }
                        break;
                    case States.Success:
                        callback(new ValueResult<Block>(block, waitHandle));
                        _scheduler.ScheduleWorkItem(() => writerStateMachine(States.Finalize, CancellationToken.None));
                        break;
                    case States.Error:
                        callback(new ExceptionResult<Block>(exception, waitHandle));
                        _scheduler.ScheduleWorkItem(() => writerStateMachine(States.Finalize, CancellationToken.None));
                        break;
                    case States.Finalize:
                        RemoveAwaiter(waitHandle);
                        waitHandle.Set();
                        break;
                    default: throw new ArgumentOutOfRangeException($"Unknown state: {state}");
                }
            };
            _scheduler.ScheduleWorkItem(() => writerStateMachine(States.Start, cancellationToken));
            StoreAwaiter(waitHandle);

            return waitHandle;
        }

        private void StoreAwaiter(WaitHandle awaiter)
        {
            lock (_locker) {
                _awaiters.Add(awaiter);
            }
        }

        private void RemoveAwaiter(WaitHandle awaiter)
        {
            lock (_locker) {
                _awaiters.Remove(awaiter);
            }
        }

        private Stream GetStream()
        {
            lock (_locker) {
                if (_handlersPool.Count == 0) {
                    return CreateNewStream(_target);
                }
                var stream = _handlersPool.Pop();
                return stream;
            }
        }

        private void TakeBackStream(Stream stream)
        {
            lock (_locker) {
                _handlersPool.Push(stream);
            }
        }

        private Stream CreateNewStream(string filename)
        {
            return new FileStream(filename, FileMode.Open, FileAccess.Write, FileShare.Write, bufferSize: Environment.SystemPageSize);
        }

        private void WriteBlock(byte[] data, long offset, int bytesCount)
        {
            var stream = GetStream();
            try {
                stream.Position = offset;
                stream.Write(data, 0, bytesCount);
                stream.Flush();
            } finally {
                TakeBackStream(stream);
            }
        }

        protected override void DisposeCore()
        {
            if (_awaiters.Count > 0) {
                WaitHandle.WaitAll(_awaiters.ToArray());
            }
            for (int counter = 0; counter < _handlersPool.Count; counter++) {
                var handler = _handlersPool.Pop();
                handler?.Dispose();
            }

            base.DisposeCore();
        }
    }
}
