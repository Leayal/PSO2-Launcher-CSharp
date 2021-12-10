using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Toolbox
{
    public struct LogReaderDataReceivedEventArgs
    {
        private const char DataSplitter = '\t';
        public readonly string Data;

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



        public LogReaderDataReceivedEventArgs(string data)
        {
            this.Data = data;
        }
    }
}
