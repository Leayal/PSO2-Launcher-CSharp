using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>Event data for <seealso cref="PSO2LogAsyncListener.DataReceived"/>.</summary>
    public readonly struct PSO2LogData
    {
        private const char DataSplitter = '\t';

        /// <summary>The raw data (a log line) that is fetched from the file.</summary>
        public readonly string Data;

        /// <summary>Split the log line into parts by their space/tab.</summary>
        /// <returns>A list of text parts</returns>
        public IReadOnlyList<ReadOnlyMemory<char>> GetDataColumns()
        {  
            var span = this.Data.AsSpan();
            if (!span.IsEmpty)
            {
                int len = 1;
                int i = 0;
                for (; i < span.Length; i++)
                {
                    if (span[i] == DataSplitter)
                    {
                        len++;
                    }
                }
                var arr = new ReadOnlyMemory<char>[len];
                i = 0;
                foreach (var c in this.EnumerateDataColumns())
                {
                    arr[i++] = c;
                }
                return arr;
            }
            else
            {
                return Array.Empty<ReadOnlyMemory<char>>();
            }
        }

        // <summary>Split the log line into parts by their space/tab.</summary>
        /// <returns>A list of text parts</returns>
        public IEnumerable<ReadOnlyMemory<char>> EnumerateDataColumns()
        {
            // var result = new List<ReadOnlyMemory<char>>(8);
            var mem = this.Data.AsMemory();
            var i = mem.Span.IndexOf(DataSplitter);
            while (i != -1)
            {
                var acquired = mem.Slice(0, i);
                // result.Add(acquired);
                yield return acquired;
                mem = mem.Slice(i + 1);
                i = mem.Span.IndexOf(DataSplitter);
            }
            // result.Add(mem);
            // return result;
            yield return mem;
        }

        /// <summary>Create a new event data from the raw log line.</summary>
        /// <param name="data">The log line which is read from the log file.</param>
        public PSO2LogData(string data)
        {
            this.Data = data;
        }
    }
}
