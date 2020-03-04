﻿using GZipTest.Arch.Abstract;
using GZipTest.Arch.Model;
using GZipTest.Arch.Results;
using GZipTest.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest.Arch
{
    internal sealed class Decompressor : DisposableObject, IReader<Block>, IDisposable
    {
        private enum States
        {
            Start,
            Decompress,
            Success,
            EndOfStream,
            Error,
            Finalize
        }

        private readonly IReader<Block> _reader;
        private readonly CompressionLevel _compressionLevel;
        private readonly IArchScheduler _scheduler;

        public Decompressor(IReader<Block> reader, CompressionLevel compressionLevel, IArchScheduler archScheduler)
        {
            ThrowIf.Argument.IsNull(reader, nameof(reader));
            ThrowIf.Argument.IsNull(archScheduler, nameof(archScheduler));

            _reader = reader;
            _compressionLevel = compressionLevel;
            _scheduler = archScheduler;
        }

        public WaitHandle ReadAsync(Action<IAsyncResult<Block>> callback, CancellationToken cancellationToken)
        {
            var waitHandle = new ManualResetEvent(initialState: false);

            Action<States, CancellationToken> readerStateMachine = null;
            Block block = null;
            Block compressedBlock = null;
            Exception exception = null;
            readerStateMachine = (state, token) => {
                if (token.IsCancellationRequested) {
                    waitHandle.Set();
                    return;
                }

                switch (state) {
                    case States.Start:
                        _reader.ReadAsync(blockAsyncResult => {
                            try {
                                block = blockAsyncResult.Result;
                                if (block.Index >= 0) {
                                    _scheduler.ScheduleWorkItem(() => readerStateMachine(States.Decompress, token));
                                } else {
                                    _scheduler.ScheduleWorkItem(() => readerStateMachine(States.EndOfStream, token));
                                }
                            } catch (Exception ex) {
                                exception = ex;
                                _scheduler.ScheduleWorkItem(() => readerStateMachine(States.Error, token));
                            }
                        }, token);
                        break;
                    case States.EndOfStream:
                        callback(new ValueResult<Block>(block, waitHandle));
                        _scheduler.ScheduleWorkItem(() => readerStateMachine(States.Finalize, CancellationToken.None));
                        break;
                    case States.Decompress: // compress
                        try {
                            int size;
                            byte[] data;
                            (size, data) = DecompressBlock(block.Payload, block.Size, _compressionLevel);
                            compressedBlock = new Block(block.Index, block.Capacity, data, size);

                            _scheduler.ScheduleWorkItem(() => readerStateMachine(States.Success, token));
                        } catch (Exception ex) {
                            exception = ex;
                            _scheduler.ScheduleWorkItem(() => readerStateMachine(States.Error, token));
                        }
                        break;
                    case States.Success: // success
                        callback(new ValueResult<Block>(compressedBlock, waitHandle));
                        _scheduler.ScheduleWorkItem(() => readerStateMachine(States.Finalize, CancellationToken.None));
                        break;
                    case States.Error: // error
                        callback(new ExceptionResult<Block>(exception, waitHandle));
                        _scheduler.ScheduleWorkItem(() => readerStateMachine(States.Finalize, CancellationToken.None));
                        break;
                    case States.Finalize: // finalize
                        waitHandle.Set();
                        break;
                }
            };
            _scheduler.ScheduleWorkItem(() => readerStateMachine(0, cancellationToken));

            return waitHandle;
        }

        private static (int, byte[]) DecompressBlock(byte[] data, int blockSize, CompressionLevel compression)
        {
            if (data.Length != blockSize) {
                data = data.Take(blockSize).ToArray();
            }
            using (var source = new MemoryStream(data)) {
                using (var compressed = new MemoryStream()) {
                    //compression
                    using (var compressor = new GZipStream(compressed, compression, leaveOpen: true)) {
                        byte[] buffer = new byte[Environment.SystemPageSize];
                        int readBytes;
                        while ((readBytes = source.Read(buffer, offset: 0, buffer.Length)) > 0) {
                            compressor.Write(buffer, offset: 0, count: readBytes);
                        }
                    }

                    var outputArray = compressed.ToArray();
                    return (outputArray.Length, outputArray);
                }
            }
        }

        private static (int, byte[]) CompressBlock(byte[] data, int blockSize, CompressionLevel compression)
        {
            if (data.Length != blockSize) {
                data = data.Take(blockSize).ToArray();
            }
            using (var source = new MemoryStream(data)) {
                using (var compressed = new MemoryStream()) {
                    //compression
                    using (var compressor = new GZipStream(compressed, compression, leaveOpen: true)) {
                        byte[] buffer = new byte[Environment.SystemPageSize];
                        int readBytes;
                        while ((readBytes = source.Read(buffer, offset: 0, buffer.Length)) > 0) {
                            compressor.Write(buffer, offset: 0, count: readBytes);
                        }
                    }

                    var outputArray = compressed.ToArray();
                    return (outputArray.Length, outputArray);
                }
            }
        }

        protected override void DisposeCore()
        {
            _reader.Dispose();
            base.DisposeCore();
        }
    }
}
