using System;
using System.IO;
using System.IO.Compression;

namespace Knapcode.SocketToMe.Support
{
    /// <summary>
    /// A stream the inverts a wrapper stream from writing to reading. The primary purpose for this class is to allow
    /// the a <see cref="GZipStream"/> intended for compression to be read from, instead of written to. This allows for
    /// easier chaining of streams.
    /// </summary>
    public class InvertedStream : Stream
    {
        private readonly BufferStream _bufferStream;
        private readonly bool _leaveOpen;
        private readonly Stream _sourceStream;
        private readonly Stream _wrapperStream;
        private bool _complete;

        public InvertedStream(Stream source, Func<Stream, Stream> wrapStream) : this(source, wrapStream, false)
        {
        }

        public InvertedStream(Stream source, Func<Stream, Stream> wrapStream, bool leaveOpen)
        {
            _sourceStream = source;
            _bufferStream = new BufferStream();
            _wrapperStream = wrapStream(_bufferStream);
            _leaveOpen = leaveOpen;
            _complete = false;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override long Length
        {
            get { throw new NotSupportedException("This operation is not supported."); }
        }

        public override long Position
        {
            get { throw new NotSupportedException("This operation is not supported."); }
            set { throw new NotSupportedException("This operation is not supported."); }
        }

        protected override void Dispose(bool disposing)
        {
            _bufferStream.IgnoreDispose(false);
            _wrapperStream.Dispose();
            if (!_leaveOpen)
            {
                _sourceStream.Dispose();
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException("This operation is not supported.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("This operation is not supported.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("This operation is not supported.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            while (true)
            {
                if (_bufferStream.Position < _bufferStream.Length)
                {
                    return _bufferStream.Read(buffer, offset, count);
                }

                if (_complete)
                {
                    return 0;
                }

                _bufferStream.SetLength(0);
                var read = _sourceStream.Read(buffer, offset, count);
                if (read == 0)
                {
                    _wrapperStream.Close();
                    _bufferStream.Position = 0;
                    _complete = true;
                }
                else
                {
                    _wrapperStream.Write(buffer, offset, read);
                    _bufferStream.Position = 0;
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("This operation is not supported.");
        }

        private class BufferStream : MemoryStream
        {
            protected override void Dispose(bool disposing)
            {
                IgnoreDispose(true);
            }

            public void IgnoreDispose(bool ignore)
            {
                if (!ignore)
                {
                    base.Dispose(true);
                }
            }
        }
    }
}