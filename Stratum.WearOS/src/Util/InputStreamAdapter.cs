// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Java.IO;

namespace Stratum.WearOS.Util
{
    public class InputStreamAdapter : Stream
    {
        private readonly InputStream _inputStream;

        public InputStreamAdapter(InputStream inputStream)
        {
            _inputStream = inputStream;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        private static int AdaptBytesReadForEof(int bytesRead)
        {
            return bytesRead == -1 ? 0 : bytesRead;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return AdaptBytesReadForEof(_inputStream.Read(buffer, offset, count));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.Run(async () => AdaptBytesReadForEof(await _inputStream.ReadAsync(buffer, offset, count)), cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }
}