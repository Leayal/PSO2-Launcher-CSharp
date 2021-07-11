using System;
using System.IO;
using System.Text;
using System.Buffers;

namespace Leayal.Shared
{
    public static class FileHelper
    {
        public static ReadOnlyMemory<char> ReadAllTexts(string filepath, Encoding encoding)
        {
            using (var fs = File.OpenRead(filepath))
            using (var stream = new StreamReader(fs, encoding))
                return ReadAllTexts(stream, stream.CurrentEncoding, fs.Length);
        }

        public static ReadOnlyMemory<char> ReadAllTexts(string filepath)
        {
            using (var fs = File.OpenRead(filepath))
            using (var stream = new StreamReader(fs))
                return ReadAllTexts(stream, stream.CurrentEncoding, fs.Length);
        }

        private static ReadOnlyMemory<char> ReadAllTexts(TextReader reader, Encoding encoding, long length)
        {
            ArrayBufferWriter<char> buffer;
            if (encoding.IsSingleByte)
            {
                buffer = new ArrayBufferWriter<char>((int)length);
            }
            else
            {
                double len = length / 2;
                int bufferLenth = (int)Math.Ceiling(len);
                buffer = new ArrayBufferWriter<char>(bufferLenth);
            }
            int readCount = reader.ReadBlock(buffer.GetSpan());
            while (readCount > 0)
            {
                buffer.Advance(readCount);
                readCount = reader.ReadBlock(buffer.GetSpan());
            }

            return buffer.WrittenMemory;
        }

        public static Memory<char> TakeAllTexts(string filepath, Encoding encoding)
        {
            using (var fs = File.OpenRead(filepath))
            using (var stream = new StreamReader(fs, encoding))
                return TakeAllTexts(stream, stream.CurrentEncoding, fs.Length);
        }

        public static Memory<char> TakeAllTexts(string filepath)
        {
            using (var fs = File.OpenRead(filepath))
            using (var stream = new StreamReader(fs))
                return TakeAllTexts(stream, stream.CurrentEncoding, fs.Length);
        }

        private static Memory<char> TakeAllTexts(TextReader reader, Encoding encoding, long length)
        {
            char[] buffer;
            if (encoding.IsSingleByte)
            {
                buffer = new char[length];
            }
            else
            {
                double len = length / 2;
                int bufferLenth = (int)Math.Ceiling(len);
                buffer = new char[bufferLenth];
            }
            var span = buffer.AsSpan();
            int offset = 0;
            int readCount = reader.ReadBlock(span);
            while (readCount > 0)
            {
                offset += readCount;
                readCount = reader.ReadBlock(span.Slice(offset));
            }

            return new Memory<char>(buffer, 0, offset);
        }
    }
}
