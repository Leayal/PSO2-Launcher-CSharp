using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>Event data for <seealso cref="PSO2LogAsyncReader.DataReceived"/>.</summary>
    public readonly struct LogReaderDataReceivedEventArgs
    {
        private const char DataSplitter = '\t';

        /// <summary>The raw data (a log line) that is fetched from the file.</summary>
        public readonly string Data;

        /// <summary>Split the log line into parts by their space/tab.</summary>
        /// <returns>A list of text parts</returns>
        public List<ReadOnlyMemory<char>> GetDatas()
        {
            var result = new List<ReadOnlyMemory<char>>(8);
            var mem = this.Data.AsMemory();
            var i = mem.Span.IndexOf(DataSplitter);
            while (i != -1)
            {
                var acquired = mem.Slice(0, i);
                result.Add(acquired);
                mem = mem.Slice(i + 1);
                i = mem.Span.IndexOf(DataSplitter);
            }
            result.Add(mem);
            return result;
        }

        /// <summary>Create a new event data from the raw log line.</summary>
        /// <param name="data">The log line which is read from the log file.</param>
        public LogReaderDataReceivedEventArgs(string data)
        {
            this.Data = data;
        }
    }
}
