using System;
using System.IO;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Support
{
    public partial class ByteStreamReader
    {
        public string ReadLine()
        {
            EnsureFirstRead();

            if (_bufferSize == 0)
            {
                return null;
            }

            var lineStream = new MemoryStream();
            int lineEndingPosition = 0;
            bool lineFinished = false;
            while (lineEndingPosition < _lineEndingBuffer.Length && _bufferSize > 0)
            {
                int endPosition;
                for (endPosition = _position; endPosition < _bufferSize; endPosition++)
                {
                    if (_buffer[endPosition] == _lineEndingBuffer[lineEndingPosition])
                    {
                        lineEndingPosition++;
                        if (lineEndingPosition == _lineEndingBuffer.Length)
                        {
                            endPosition++;
                            lineFinished = true;
                            break;
                        }
                    }
                    else if (lineEndingPosition > 0)
                    {
                        lineEndingPosition = 0;
                    }
                }

                lineStream.Write(_buffer, _position, endPosition - _position);
                _position = endPosition;

                if (endPosition == _bufferSize && !lineFinished)
                {
                    _bufferSize = _stream.Read(_buffer, 0, _buffer.Length);
                    _position = 0;
                }
            }

            var line = _encoding.GetString(lineStream.GetBuffer(), 0, (int) lineStream.Length);
            if (!_preserveLineEndings && lineFinished)
            {
                line = line.Substring(0, line.Length - _lineEnding.Length);
            }

            return line;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            if (_bufferSize >= 0)
            {
                read = Math.Min(count, _bufferSize - _position);
                Buffer.BlockCopy(_buffer, _position, buffer, offset, read);
                count -= read;
                offset += read;
                _position += read;

                if (_position == _bufferSize)
                {
                    _bufferSize = -1;
                }
            }

            if (count != 0)
            {
                read += _stream.Read(buffer, offset, count);
            }

            return read;
        }

        private void EnsureFirstRead()
        {
            if (_bufferSize < 0)
            {
                _bufferSize = _stream.Read(_buffer, 0, _buffer.Length);
            }
        }

        public async Task<string> ReadLineAsync()
        {
            await EnsureFirstReadAsync().ConfigureAwait(false);

            if (_bufferSize == 0)
            {
                return null;
            }

            var lineStream = new MemoryStream();
            int lineEndingPosition = 0;
            bool lineFinished = false;
            while (lineEndingPosition < _lineEndingBuffer.Length && _bufferSize > 0)
            {
                int endPosition;
                for (endPosition = _position; endPosition < _bufferSize; endPosition++)
                {
                    if (_buffer[endPosition] == _lineEndingBuffer[lineEndingPosition])
                    {
                        lineEndingPosition++;
                        if (lineEndingPosition == _lineEndingBuffer.Length)
                        {
                            endPosition++;
                            lineFinished = true;
                            break;
                        }
                    }
                    else if (lineEndingPosition > 0)
                    {
                        lineEndingPosition = 0;
                    }
                }

                lineStream.Write(_buffer, _position, endPosition - _position);
                _position = endPosition;

                if (endPosition == _bufferSize && !lineFinished)
                {
                    _bufferSize = await _stream.ReadAsync(_buffer, 0, _buffer.Length).ConfigureAwait(false);
                    _position = 0;
                }
            }

            var line = _encoding.GetString(lineStream.GetBuffer(), 0, (int) lineStream.Length);
            if (!_preserveLineEndings && lineFinished)
            {
                line = line.Substring(0, line.Length - _lineEnding.Length);
            }

            return line;
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            int read = 0;
            if (_bufferSize >= 0)
            {
                read = Math.Min(count, _bufferSize - _position);
                Buffer.BlockCopy(_buffer, _position, buffer, offset, read);
                count -= read;
                offset += read;
                _position += read;

                if (_position == _bufferSize)
                {
                    _bufferSize = -1;
                }
            }

            if (count != 0)
            {
                read += await _stream.ReadAsync(buffer, offset, count).ConfigureAwait(false);
            }

            return read;
        }

        private async Task EnsureFirstReadAsync()
        {
            if (_bufferSize < 0)
            {
                _bufferSize = await _stream.ReadAsync(_buffer, 0, _buffer.Length).ConfigureAwait(false);
            }
        }
    }
}