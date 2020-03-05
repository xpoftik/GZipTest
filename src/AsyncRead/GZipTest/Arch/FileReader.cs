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
    /// Async file reader.
    /// 
    /// </summary>
    internal sealed class FileReader : DisposableObject, IReader<Block>, IDisposable
    {
        private object _locker = new object();
        private readonly IArchScheduler _scheduler;
        
        private readonly string _target;
        private readonly int _blockSizeInBytes;
        private Stack<Stream> _handlersPool = new Stack<Stream>();
        private List<WaitHandle> _awaiters = new List<WaitHandle>();
        
        private int _counter = -1;

        public FileReader(string target, IArchScheduler scheduler, int blockSize = Consts.DEFAULT_BLOCK_SIZE)
        {
            ThrowIf.Argument.StringIsEmpty(target, nameof(target));
            if (!File.Exists(target)) {
                throw new FileNotFoundException("File not found.", target);
            }
            ThrowIf.Argument.IsNull(scheduler, nameof(scheduler));
            ThrowIf.Argument.LessOrEqualZero(blockSize, nameof(blockSize));

            _target = target;
            _scheduler = scheduler;
            _blockSizeInBytes = blockSize;
        }

        public WaitHandle ReadAsync(Action<IAsyncResult<Block>> callback, CancellationToken cancellationToken)
        {
            var waitHandle = new ManualResetEvent(initialState: false);
            Action<int, CancellationToken> readStateMachine = null;
            Block block = null;
            Exception exception = null;
            readStateMachine = (state, token) => {
                if (token.IsCancellationRequested) {
                    waitHandle.Set();
                    return;
                }

                switch (state) {
                    case 0:
                        var blockIdx = GetNextBlockIndex();
                        var offset = (long)blockIdx * _blockSizeInBytes;
                        _scheduler.ScheduleWorkItem(
                            workItem: () => {
                                try {
                                    block = ReadBlock(blockIdx, offset);
                                } catch (Exception ex) {
                                    exception = ex;
                                }
                            },
                            callback: () => {
                                if (block != null) {
                                    readStateMachine(1, token);
                                } else {
                                    readStateMachine(2, token);
                                }
                            });

                        break;
                    case 1: // success
                        _scheduler.ScheduleWorkItem(
                            workItem: () => callback(new ValueResult<Block>(block, waitHandle)),
                            callback: () => readStateMachine(3, token));
                        break;
                    case 2: // exception
                        _scheduler.ScheduleWorkItem(
                            workItem: () => callback(new ExceptionResult<Block>(exception, waitHandle)),
                            callback: () => readStateMachine(3, token));
                        break;
                    case 3: // finalizing
                        RemoveAwaiter(waitHandle);
                        waitHandle.Set();
                        break;
                }
            };

            _scheduler.ScheduleWorkItem(() => readStateMachine(0, cancellationToken), callback: null);
            StoreAwaiter(waitHandle);

            return waitHandle;
        }

        private int GetNextBlockIndex() {
            return Interlocked.Increment(ref _counter);
        }

        private void StoreAwaiter(WaitHandle awaiter)
        {
            lock (_locker) {
                _awaiters.Add(awaiter);
            }
        }

        private void RemoveAwaiter(WaitHandle awaiter) {
            lock (_locker) {
                _awaiters.Remove(awaiter);
            }
        }

        private Stream GetStream() {
            lock (_locker) {
                if (_handlersPool.Count == 0) {
                    return CreateNewStream(_target);
                }
                var stream = _handlersPool.Pop();
                return stream;
            }
        }

        private void TakeBackStream(Stream stream) {
            lock (_locker) {
                _handlersPool.Push(stream);
            }
        }

        private Stream CreateNewStream(string filename)
        {
            return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: Environment.SystemPageSize, options: FileOptions.RandomAccess);
        }

        private Block ReadBlock(int index, long offset) {
            var stream = GetStream();
            try {
                //if (index > 100) {
                //    throw new Exception("TEST EXCEPTION!!!");
                //}

                byte[] buffer = new byte[_blockSizeInBytes];
                stream.Position = offset;
                var readBytes = stream.Read(buffer, offset: 0, count: _blockSizeInBytes);
                Block block;
                if (readBytes == 0) {
                    block = Block.NullBlock();
                } else {
                    block = new Block(index, capacity: _blockSizeInBytes, payload: buffer, size: readBytes);
                }
                return block;
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
