using System.IO;
using System.Text;

namespace Knapcode.SocketToMe.Support
{
    public partial class ByteStreamReader
    {
        private readonly Stream _stream;
        private readonly bool _preserveLineEndings;
        private readonly Encoding _encoding;
        private readonly string _lineEnding;
        private readonly byte[] _lineEndingBuffer;
        private readonly byte[] _buffer;

        private int _position;
        private int _bufferSize;

        public ByteStreamReader(Stream stream, int bufferSize, bool preserveLineEndings)
        {
            _stream = stream;
            _preserveLineEndings = preserveLineEndings;
            _encoding = new UTF8Encoding(false);
            _lineEnding = "\r\n";
            _lineEndingBuffer = _encoding.GetBytes("\r\n");
            _buffer = new byte[bufferSize];

            _position = 0;
            _bufferSize = -1;
        }

        public Stream GetRemainingStream()
        {
            return new PartiallyBufferedStream(_buffer, _position, _bufferSize - _position, _stream);
        }
    }
}