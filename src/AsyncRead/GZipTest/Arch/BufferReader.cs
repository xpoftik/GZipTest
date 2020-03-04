using GZipTest.Arch.Abstract;
using GZipTest.Arch.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using GZipTest.Utils;
using GZipTest.Arch.Results;

namespace GZipTest.Arch
{
    internal sealed class BufferReader : DisposableObject, IReader<Block>, IDisposable
    {
        private object _locker = new object();

        private readonly IReader<Block> _reader;
        private readonly int _bufferSizeLimitInBytes;
        private readonly IArchScheduler _scheduler;

        private bool _isBufferingInProcess = false;
        private bool _isFaulted => _errors.Count > 0;
        private List<Exception> _errors = new List<Exception>();

        private bool _endOfStream = false;

        private Queue<Block> _buffer = new Queue<Block>();

        public BufferReader(IReader<Block> reader, int bufferSizeLimitInBytes, IArchScheduler scheduler)
        {
            ThrowIf.Argument.IsNull(reader, nameof(reader));
            if (bufferSizeLimitInBytes <= 0) {
                bufferSizeLimitInBytes = Consts.DEFAULT_BUFFER_SIZE_LIMIT;
            }

            _reader = reader;
            _bufferSizeLimitInBytes = bufferSizeLimitInBytes;
            _scheduler = scheduler;

            BeginBufferingIfNeed();
        }

        public WaitHandle ReadAsync(Action<IAsyncResult<Block>> callback, CancellationToken cancellationToken = default)
        {
            ThrowIf.Argument.IsNull(callback, nameof(callback));

            var waitHandle = new ManualResetEvent(initialState: false);
            Action<int, CancellationToken> readStateMachine = null;
            Exception exception = null ;
            Block block = null ;
            readStateMachine = (state, token) => {
                //Console.WriteLine($"Reading state machine: {state}");
                if (token.IsCancellationRequested) {
                    return;
                }

                switch (state) {
                    case 0:
                        if (_isFaulted) {
                            exception = new AggregateException(GetExceptions());
                            _scheduler.ScheduleWorkItem(() => readStateMachine(4, CancellationToken.None));
                            
                            return;
                        }

                        // try to get block
                        if (TryDequeue(out Block _block)) {
                            block = _block;
                            _scheduler.ScheduleWorkItem(() => readStateMachine(1, token));

                            return;
                        }
                        
                        if (_endOfStream) {
                            _scheduler.ScheduleWorkItem(() => readStateMachine(3, token));
                        } else {
                            _scheduler.ScheduleWorkItem(() => readStateMachine(2, token));
                        }
                        break;
                    case 1: // success
                        callback(new ValueResult<Block>(block, waitHandle));
                        _scheduler.ScheduleWorkItem(
                            workItem: () => readStateMachine(5, CancellationToken.None));
                        break;
                    case 2: // retry
                        _scheduler.ScheduleWorkItem(() => readStateMachine(0, token));
                        break;
                    case 3: // end of steam
                        callback(new ValueResult<Block>(Block.NullBlock(), waitHandle));
                        _scheduler.ScheduleWorkItem(
                            workItem: () => readStateMachine(5, CancellationToken.None)) ;
                        break;
                    case 4: // exception
                        callback(new ExceptionResult<Block>(exception, waitHandle));
                        _scheduler.ScheduleWorkItem(
                            workItem: () => readStateMachine(5, CancellationToken.None));
                        break;
                    case 5: // finalizing
                        waitHandle.Set();
                        break;
                }
            };

            _scheduler.ScheduleWorkItem(() => readStateMachine(0, cancellationToken));

            return waitHandle;
        }

        private WaitHandle BeginBufferingIfNeed(CancellationToken cancellationToken = default) {
            lock (_locker) {
                if (_isBufferingInProcess) return new ManualResetEvent(initialState: true);
                _isBufferingInProcess = true;
                
                var waitHandle = new ManualResetEvent(initialState: false);

                // start buffering
                Action<int, CancellationToken> bufferingStateMachine = null;
                Block block = null;
                Exception exception = null;
                bufferingStateMachine = (state, token) => {
                    //Console.WriteLine($"Buffering state: {state}");
                    if (token.IsCancellationRequested) {
                        return;
                    }

                    switch (state) {
                        case 0:
                            _reader.ReadAsync(asyncResult => {
                                try {
                                    block = asyncResult.Result;
                                    _scheduler.ScheduleWorkItem(() => bufferingStateMachine(1, token));
                                } catch (Exception ex) {
                                    exception = ex;
                                    _scheduler.ScheduleWorkItem(() => bufferingStateMachine(2, CancellationToken.None));
                                }
                            }, token);
                            break;
                        case 1: // success
                            if (block.Index < 0) {
                                _endOfStream = true;
                                _scheduler.ScheduleWorkItem(() => bufferingStateMachine(3, CancellationToken.None));

                                return;
                            }

                            Enqueue(block);
                            if (GetCurrentBufferSize() < _bufferSizeLimitInBytes) {
                                _scheduler.ScheduleWorkItem(() => bufferingStateMachine(0, token)); // get next element
                            } else {
                                _scheduler.ScheduleWorkItem(() => bufferingStateMachine(4, CancellationToken.None)); // waiting spinner
                            }
                            break;
                        case 2: // exception
                            SetException(exception);
                            _scheduler.ScheduleWorkItem(() => bufferingStateMachine(3, CancellationToken.None));
                            break;
                        case 3: // finalizing
                            _isBufferingInProcess = false;
                            waitHandle.Set();
                            break;
                        case 4: // waiting spinner
                            if (GetCurrentBufferSize() > _bufferSizeLimitInBytes) {
                                _scheduler.ScheduleWorkItem(() => bufferingStateMachine(4, token));
                            } else {
                                _scheduler.ScheduleWorkItem(() => bufferingStateMachine(0, token));
                            }
                            break;
                    }
                };
                _scheduler.ScheduleWorkItem(() => bufferingStateMachine(0, cancellationToken));
                
                return waitHandle;
            }
        }


        private void Enqueue(Block block) {
            lock (_locker) {
                _buffer.Enqueue(block);
            }
        }

        private bool TryDequeue(out Block block) {
            bool lockTaken = false;
            Monitor.TryEnter(_locker, ref lockTaken);
            block = null;
            if (lockTaken) {
                try {
                    if (_buffer.Count > 0) {
                        block = _buffer.Dequeue();
                    }
                } finally {
                    Monitor.Exit(_locker);
                }
            }
            return block != null;
        }

        private long GetCurrentBufferSize() {
            lock (_locker) {
                return _buffer.Sum(b => b.Size);
            }
        }

        private void SetException(Exception exception) {
            lock (_locker) {
                _errors.Add(exception);
            }
        }

        private IEnumerable<Exception> GetExceptions() {
            lock (_locker) {
                return _errors.ToArray();
            }
        }

        protected override void DisposeCore()
        {
            _reader.Dispose();
            base.DisposeCore();
        }
    }
}
