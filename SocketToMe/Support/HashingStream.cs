using System;
using System.IO;
using System.Security.Cryptography;

namespace Knapcode.SocketToMe.Support
{
    public class HashingStream : Stream
    {
        private readonly HashAlgorithm _hashAlgorithm;
        private readonly Stream _innerStream;
        private readonly bool _read;
        private byte[] _hash;

        public HashingStream(Stream innerStream, HashAlgorithm hashAlgorithm, bool read)
        {
            _innerStream = innerStream;
            _hashAlgorithm = hashAlgorithm;
            _read = read;
        }

        public byte[] Hash => (byte[]) _hash?.Clone();

        public override bool CanRead => _read;
        public override bool CanSeek => false;
        public override bool CanWrite => !_read;

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_read || (_read && _hash == null))
            {
                FinishHash();
            }

            _hashAlgorithm.Dispose();
            _innerStream.Dispose();
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_read)
            {
                throw new NotSupportedException("The hashing stream is configured to write, not read.");
            }

            var read = _innerStream.Read(buffer, offset, count);
            if (read > 0)
            {
                _hashAlgorithm.TransformBlock(buffer, offset, read, null, -1);
            }
            else
            {
                FinishHash();
            }

            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_read)
            {
                throw new NotSupportedException("The hashing stream is configured to read, not write.");
            }

            _hashAlgorithm.TransformBlock(buffer, offset, count, null, -1);
            _innerStream.Write(buffer, offset, count);
        }

        private void FinishHash()
        {
            _hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
            _hash = _hashAlgorithm.Hash;
        }
    }
}