using GZipTest.Arch.Abstract;
using GZipTest.Arch.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;

namespace GZipTest.Arch
{
    internal sealed class BufferReader : IReader<Block>
    {
        private object _locker = new object();

        private readonly IReader<Block> _reader;
        private readonly int _bufferSizeLimitInBytes;
        private readonly IArchScheduler _scheduler;

        private readonly int _requestLimit = 4;
        private int _requestCount = 0;
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
        }

        public WaitHandle ReadAsync(Action<Block> callback, CancellationToken cancellationToken)
        {
            Action buffering = null;
            //TODO: Buffering state machine
            buffering = () => {
                while (_requestCount < _requestLimit) {
                    _requestCount = Interlocked.Increment(ref _requestCount);
                    _scheduler.ScheduleWorkItem(
                        //workItem
                        () => {
                            _reader.ReadAsync(_block => {
                                Interlocked.Decrement(ref _requestCount);

                                Enqueue(_block);

                                if (_block.Index >= 0) {
                                    if (GetCurrentBufferSize() < _bufferSizeLimitInBytes) {
                                        _scheduler.ScheduleWorkItem(buffering, callback: null);
                                    }
                                } else {
                                    _endOfStream = true;
                                }
                            }, CancellationToken.None);
                        },
                        callback: null);
                }
            };
            _scheduler.ScheduleWorkItem(buffering, callback: null);

            Action readAsyncSpinner = null;
            WaitHandle spinnerWaitHandler = null;
            readAsyncSpinner = () => {
                if (TryDequeue(out Block block)) {
                    //callback(block);
                    _scheduler.ScheduleWorkItem(() => callback(block), null);
                } else {
                    //TODO: reschedule
                    //_scheduler.RescheduleWorkItem(readAsyncSpinner, null, spinnerWaitHandler);
                    if (!_endOfStream) {
                        _scheduler.ScheduleWorkItem(readAsyncSpinner, null);
                    }
                }
            };
            spinnerWaitHandler = _scheduler.ScheduleWorkItem(readAsyncSpinner, null);
            
            return spinnerWaitHandler;
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
    }
}
