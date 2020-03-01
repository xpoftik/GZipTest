using GZipTest.Arch.Abstract;
using GZipTest.Arch.Model;
using GZipTest.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace GZipTest.Arch
{
    /// <summary>
    /// File reader.
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

        public WaitHandle ReadAsync(Action<Block> callback, CancellationToken cancellationToken)
        {
            var waitHandle = _scheduler.ScheduleWorkItem(() => {
                cancellationToken.ThrowIfCancellationRequested();
                
                var stream = GetStream();
                try {
                    var index = Interlocked.Increment(ref _counter);
                    var offset = (long)index * _blockSizeInBytes;

                    var block = ReadBlock(stream, index, offset);

                    return block;
                } finally {
                    TakeBackStream(stream);
                }
            },
            _block => callback(_block));

            StoreAwaiter(waitHandle);

            return waitHandle;
        }

        private void StoreAwaiter(WaitHandle awaiter)
        {
            lock (_locker) {
                _awaiters.Add(awaiter);
            }
        }

        //TODO: Remove this method or use!
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
            return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        private Block ReadBlock(Stream stream, int index, long offset) {
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
        }

        protected override void DisposeCore()
        {
            //TODO: дождаться пока все запланированные операции будут выполнены, затем закрыть все потоки из пула, очистить пул.
            WaitHandle.WaitAll(_awaiters.ToArray());
            for (int counter = 0; counter < _handlersPool.Count; counter++) {
                var handler = _handlersPool.Pop();
                handler?.Dispose();
            }

            base.DisposeCore();
        }
    }
}
