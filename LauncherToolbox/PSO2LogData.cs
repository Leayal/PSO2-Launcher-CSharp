﻿namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>Event data for <seealso cref="PSO2LogAsyncListener.DataReceived"/>.</summary>
    /// <remarks>All the data within this structure will be invalid once the event occurance is finished. Thus, if you want the data's lifetime to be beyond the event occurance, allocation and copying is the only way.</remarks>
    public readonly struct PSO2LogData
    {
        private const char DataSplitter = '\t';

        private readonly List<ReadOnlyMemory<char>> workspace;

        /// <summary>The raw data (a log line) that is fetched from the file.</summary>
        public readonly ReadOnlyMemory<char> Data;

        /// <summary>Split the log line into parts by their space/tab.</summary>
        /// <returns>A list of text parts</returns>
        public IReadOnlyList<ReadOnlyMemory<char>> GetDataColumns()
        {
            if (!this.Data.IsEmpty)
            {
                this.workspace.Clear();
                this.workspace.AddRange(this.EnumerateDataColumns());
                return this.workspace;
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
            var mem = this.Data;
            var i = mem.Span.IndexOf(DataSplitter);
            while (i != -1)
            {
                var acquired = mem.Slice(0, i);
                // result.Add(acquired);
                if (!acquired.IsEmpty)
                {
                    yield return acquired;
                }
                mem = mem.Slice(i + 1);
                i = mem.Span.IndexOf(DataSplitter);
            }
            // result.Add(mem);
            // return result;
            if (!mem.IsEmpty)
            {
                yield return mem;
            }
        }

        /// <summary>Create a new event data from the raw log line.</summary>
        /// <param name="data">The log line which is read from the log file.</param>
        /// <param name="workspace"></param>
        internal PSO2LogData(string data, List<ReadOnlyMemory<char>> workspace) : this(data.AsMemory(), workspace) { }

        /// <summary>Create a new event data from the raw log line.</summary>
        /// <param name="data">The log line which is read from the log file.</param>
        /// <param name="workspace"></param>
        internal PSO2LogData(ReadOnlyMemory<char> data, List<ReadOnlyMemory<char>> workspace)
        {
            this.workspace = workspace;
            this.Data = data;
        }
    }
}
