using GZipTest.Arch.Abstract;
using GZipTest.Arch.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch
{
    /// <summary>
    /// Arch process wrapper.
    /// 
    /// </summary>
    internal sealed class ArchProcess: IArchProcess
    {
        private enum States { 
            Start,
            Write,
            NextItem,
            EndOfSteam,
            Error,
            Finalize
        }

        private object _locker = new object();

        private readonly IReader<Block> _reader;
        private readonly IWriter<Block> _writer;
        private readonly IArchScheduler _scheduler;

        private List<Exception> exceptions = new List<Exception>();
        private bool IsFaulted => exceptions.Count > 0;
        private bool IsCancelled { get; set; }

        private int _currentBlockIndex = 0;
        private long _currentOffset = 0;
        private bool _endOfStream = false;
        private int _readRequestCount = 0;
        private int _writeRequestCount = 0;

        private Dictionary<int, Block> _buffer = new Dictionary<int, Block>();

        private ManualResetEvent _waitHandle = new ManualResetEvent(initialState: false);

        public ArchProcess(IReader<Block> reader, IWriter<Block> writer, IArchScheduler archScheduler)
        {
            ThrowIf.Argument.IsNull(reader, nameof(reader));
            ThrowIf.Argument.IsNull(writer, nameof(writer));
            ThrowIf.Argument.IsNull(archScheduler, nameof(archScheduler));

            _reader = reader;
            _writer = writer;
            _scheduler = archScheduler;
        }

        public IArchResult Result => GetResult();

        public void Start(CancellationToken cancellationToken) {
            Action<States, CancellationToken> archStateMachine = null;
            archStateMachine = (state, token) => {
                if (token.IsCancellationRequested) {
                    //TODO: try to delete target file.
                    IsCancelled = true;
                    _waitHandle.Set();
                    return;
                }

                switch (state) {
                    case States.Start:
                        Interlocked.Increment(ref _readRequestCount);
                        _reader.ReadAsync(asyncBlockResult => {
                            try {
                                var block = asyncBlockResult.Result;
                                if (block.Index >= 0) {
                                    AddToBuffer(block);
                                    _scheduler.ScheduleWorkItem(() => archStateMachine(States.Write, token));
                                    _scheduler.ScheduleWorkItem(() => archStateMachine(States.NextItem, token));
                                } else {
                                    _scheduler.ScheduleWorkItem(() => archStateMachine(States.EndOfSteam, token));
                                }
                            } catch (Exception ex) {
                                SetException(ex);
                                _scheduler.ScheduleWorkItem(() => archStateMachine(States.Error, token));
                            } finally {
                                Interlocked.Decrement(ref _readRequestCount);
                            }
                        }, token);
                        break;
                    case States.Write:
                        if (TryGetNextBlock(out Block block, out long offset)) {
                            Interlocked.Increment(ref _writeRequestCount);
                            _writer.WriteAsync(block, offset, asyncBlockResult => {
                                try {
                                    var block = asyncBlockResult.Result;
                                }catch(Exception ex) {
                                    SetException(ex);
                                    _scheduler.ScheduleWorkItem(() => archStateMachine(States.Error, token));
                                } finally {
                                    Interlocked.Decrement(ref _writeRequestCount);
                                }
                            }, token);
                        }
                        break;
                    case States.NextItem:
                        archStateMachine(States.Start, cancellationToken);
                        break;
                    case States.EndOfSteam:
                        _endOfStream = true;
                        if (_readRequestCount != 0 || _writeRequestCount != 0 || _buffer.Count > 0) {
                            _scheduler.ScheduleWorkItem(() => archStateMachine(States.EndOfSteam, token)); // wait spinner
                        } else {
                            _scheduler.ScheduleWorkItem(() => archStateMachine(States.Finalize, CancellationToken.None));
                        }
                        break;
                    case States.Error:
                        //SetException(exception);
                        _scheduler.ScheduleWorkItem(() => archStateMachine(States.Finalize, CancellationToken.None));
                        break;
                    case States.Finalize:
                        _waitHandle.Set();
                        break;
                    default: throw new ArgumentOutOfRangeException($"Unknown state: {state}");
                }
            };
            _scheduler.ScheduleWorkItem(() => archStateMachine(States.Start, cancellationToken));
        }

        private void AddToBuffer(Block block)
        {
            lock (_locker) {
                _buffer[block.Index] = block;
            }
        }

        private bool TryGetNextBlock(out Block block, out long offset) {
            block = null;
            offset = -1;
            lock (_locker) {
                if (_buffer.ContainsKey(_currentBlockIndex)) {
                    block = _buffer[_currentBlockIndex];
                    offset = _currentOffset;
                    
                    _buffer.Remove(_currentBlockIndex);
                    _currentOffset += block.Size;
                    _currentBlockIndex++;
                }
            }
            return block != null;
        }

        private void SetException(Exception exception) {
            lock (_locker) {
                exceptions.Add(exception);
            }
        }

        private IArchResult GetResult() {
            _waitHandle.WaitOne();

            _reader.Dispose();
            _writer.Dispose();
            _scheduler.Dispose();

            Console.WriteLine($"Thread count: {((SimpleAchScheduler)_scheduler).ThreadCount}");

            if (IsCancelled) {
                return ArchResult.Cancelled();
            }
            if (IsFaulted) {
                var exception = new AggregateException(exceptions.ToArray());
                var message = exception.Flatten().Message;
                return ArchResult.Faulted(message);
            }
            return ArchResult.Success();
        }
    }
}
