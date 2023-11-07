using Leayal.PSO2Launcher.Toolbox;
using System.Buffers;
using System.IO;
using System.Threading;

namespace Leayal.PSO2Launcher.Core.Classes
{
    sealed class MemoryStreamPooledBackBuffer : MemoryStream
    {
        private readonly bool isfromArrayPool;
        private byte[]? buffer;

        public MemoryStreamPooledBackBuffer(byte[] buffer, int index, int count, bool isfromArrayPool) : base(buffer, index, count, false, true)
        {
            this.isfromArrayPool = isfromArrayPool;
            this.Position = 0;
            this.buffer = buffer;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (isfromArrayPool)
            {
                var borrowed = Interlocked.Exchange(ref this.buffer, null);
                if (borrowed != null)
                {
                    ArrayPool<byte>.Shared.Return(borrowed);
                }
            }
        }
    }
}
