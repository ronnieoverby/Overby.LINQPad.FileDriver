using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Overby.LINQPad.FileDriver
{
    /// <summary>
    /// Glues multiple TextReaders together.
    /// Advancing to the next reader closes the previous.
    /// The final reader can be closed by calling Close().
    /// Calling Dispose() will dispose of the reader enumerator.
    /// No readers are ever disposed, the caller can handle that.
    /// </summary>
    public class CompositeTextReader : TextReader
    {
        private readonly IEnumerator<TextReader> _textReaders;
        private TextReader _reader;

        public CompositeTextReader(IEnumerable<TextReader> textReaders)
        {
            _textReaders = textReaders.GetEnumerator();

            if (!_textReaders.MoveNext())
                throw new ArgumentException("sequence was empty", nameof(textReaders));

            _reader = _textReaders.Current;
        }

        public override async Task<string> ReadToEndAsync()
        {
            var sb = new StringBuilder(await _reader.ReadToEndAsync());

            while (NextReader())
                sb.Append(await _reader.ReadToEndAsync());

            return sb.ToString();
        }

        public override string ReadToEnd()
        {
            var sb = new StringBuilder(_reader.ReadToEnd());

            while (NextReader())
                sb.Append(_reader.ReadToEnd());

            return sb.ToString();
        }

#if NETCORE

        public override async ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var read = await _reader.ReadBlockAsync(buffer, cancellationToken);
                if (read != -1)
                    return read;

                if (!NextReader())
                    return -1;
            }
        }

        public override int ReadBlock(Span<char> buffer)
        {
            var totalRead = 0;

            while (totalRead < buffer.Length)
            {
                var read = _reader.ReadBlock(buffer[totalRead..]);
                totalRead += read;

                if (read == 0 && !NextReader())
                    break;
            }

            return totalRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            var totalRead = 0;

            while (totalRead < buffer.Length)
            {
                var read = await _reader.ReadAsync(buffer[totalRead..]);
                totalRead += read;

                if (read == 0 && !NextReader())
                    break;
            }

            return totalRead;
        }

        public override int Read(Span<char> buffer)
        {
            var totalRead = 0;

            while (totalRead < buffer.Length)
            {
                var read = _reader.Read(buffer[totalRead..]);
                totalRead += read;

                if (read == 0 && !NextReader())
                    break;
            }

            return totalRead;
        }

#endif

        public override int Read(char[] buffer, int index, int count)
        {
            var totalRead = 0;

            while (totalRead < buffer.Length)
            {
                var read = _reader.Read(buffer, totalRead, count - totalRead);
                totalRead += read;

                if (read == 0 && !NextReader())
                    break;
            }

            return totalRead;
        }

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            var totalRead = 0;

            while (totalRead < buffer.Length)
            {
                var read = _reader.ReadBlock(buffer, totalRead, count - totalRead);
                totalRead += read;

                if (read == 0 && !NextReader())
                    break;
            }

            return totalRead;
        }

        public override async Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            var totalRead = 0;

            while (totalRead < buffer.Length)
            {
                var read = await _reader.ReadAsync(buffer, totalRead, count - totalRead);
                totalRead += read;

                if (read == 0 && !NextReader())
                    break;
            }

            return totalRead;
        }

        public override async Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            while (true)
            {
                var read = await _reader.ReadBlockAsync(buffer, index, count);
                if (read != -1)
                    return read;

                if (!NextReader())
                    return -1;
            }
        }

        public override async Task<string> ReadLineAsync()
        {
            while (true)
            {
                var read = await _reader.ReadLineAsync();
                if (read != null)
                    return read;

                if (!NextReader())
                    return null;
            }
        }

        public override string ReadLine()
        {
            while (true)
            {
                var read = _reader.ReadLine();
                if (read != null)
                    return read;

                if (!NextReader())
                    return null;
            }
        }

        public override int Read()
        {
            while (true)
            {
                var read = _reader.Read();
                if (read != -1)
                    return read;

                if (!NextReader())
                    return -1;
            }
        }

        public override int Peek()
        {
            while (true)
            {
                var read = _reader.Peek();
                if (read != -1)
                    return read;

                if (!NextReader())
                    return -1;
            }
        }

        /// <summary>
        /// Closes the current reader and advances to the next reader.
        /// </summary>
        public bool NextReader()
        {
            if (_textReaders.MoveNext())
            {
                _reader.Close();
                _reader = _textReaders.Current;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Closes all readers.
        /// </summary>
        public override void Close()
        {
            while (NextReader()) { }
            _reader.Close();
        }

        protected override void Dispose(bool disposing)
        {
            using (_textReaders)
                base.Dispose(disposing);
        }
    }
}
