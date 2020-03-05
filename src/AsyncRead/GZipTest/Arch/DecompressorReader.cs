using GZipTest.Arch.Abstract;
using GZipTest.Arch.Model;
using GZipTest.Arch.Results;
using GZipTest.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace GZipTest.Arch
{
    /// <summary>
    /// Async decompress file reader.
    /// 
    /// </summary>
    internal sealed class DecompressorReader: DisposableObject, IReader<Block>, IDisposable
    {
        private object _locker = new object();
        private readonly IArchScheduler _scheduler;

        private readonly string _target;
        private readonly int _blockSizeInBytes;
        //private Stack<Stream> _handlersPool = new Stack<Stream>();
        private Stream _decompressorStream = null;
        private List<WaitHandle> _awaiters = new List<WaitHandle>();

        private int _counter = -1;

        public DecompressorReader(string target, IArchScheduler scheduler, int blockSize = Consts.DEFAULT_BLOCK_SIZE)
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
                                    block = ReadBlock(blockIdx);
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

        private int GetNextBlockIndex()
        {
            return Interlocked.Increment(ref _counter);
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
                if (_decompressorStream != null) {
                    return _decompressorStream;
                }
                _decompressorStream = CreateNewDecompressorStream(_target);
                return _decompressorStream;
            }
        }

        private Stream CreateNewDecompressorStream(string filename)
        {
            var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: Environment.SystemPageSize, options: FileOptions.RandomAccess);
            return new GZipStream(fileStream, CompressionMode.Decompress);
        }

        private Block ReadBlock(int index)
        {
            lock (_locker) {
                var stream = GetStream();
                byte[] buffer = new byte[_blockSizeInBytes];
                //stream.Position = offset;
                var readBytes = stream.Read(buffer, offset: 0, count: _blockSizeInBytes);
                Block block;
                if (readBytes == 0) {
                    block = Block.NullBlock();
                } else {
                    block = new Block(index, capacity: _blockSizeInBytes, payload: buffer, size: readBytes);
                }
                return block;
            }
        }

        protected override void DisposeCore()
        {
            if (_awaiters.Count > 0) {
                WaitHandle.WaitAll(_awaiters.ToArray());
            }
            if (_decompressorStream != null) {
                _decompressorStream.Dispose();
            }

            base.DisposeCore();
        }
    }
}
