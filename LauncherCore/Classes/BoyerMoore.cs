using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    /// <remarks>https://gist.github.com/mjs3339/0772431281093f1bca1fce2f2eca527d</remarks>
    sealed class BoyerMoore
    {
        private readonly int[] _jumpTable;
        private readonly byte[] _pattern;
        private readonly int _patternLength;

        public int PatternLength => this._patternLength;

        public BoyerMoore(byte[] pattern)
        {
            if (pattern == null)
                throw new Exception("Pattern has not been set.");

            _pattern = pattern;
            _jumpTable = new int[256];
            _patternLength = _pattern.Length;
            for (var index = 0; index < 256; index++)
                _jumpTable[index] = _patternLength;
            for (var index = 0; index < _patternLength - 1; index++)
                _jumpTable[_pattern[index]] = _patternLength - index - 1;
        }

        /*
        public void SetPattern(byte[] pattern)
        {
            _pattern = pattern;
            _jumpTable = new int[256];
            _patternLength = _pattern.Length;
            for (var index = 0; index < 256; index++)
                _jumpTable[index] = _patternLength;
            for (var index = 0; index < _patternLength - 1; index++)
                _jumpTable[_pattern[index]] = _patternLength - index - 1;
        }
        */
        public unsafe int Search(in ReadOnlySpan<byte> searchArray)
        {
            if (_patternLength > searchArray.Length)
            {
                return -1;
            }
            var index = 0;
            var limit = searchArray.Length - _patternLength;
            var patternLengthMinusOne = _patternLength - 1;
            fixed (byte* pointerToByteArray = searchArray)
            {
                fixed (byte* pointerToPattern = _pattern)
                {
                    while (index <= limit)
                    {
                        var j = patternLengthMinusOne;
                        while (j >= 0 && pointerToPattern[j] == pointerToByteArray[index + j])
                            j--;
                        if (j < 0)
                            return index;
                        index += Math.Max(_jumpTable[pointerToByteArray[index + j]] - _patternLength + 1 + j, 1);
                    }
                }
            }
            return -1;
        }
        public unsafe IReadOnlyList<int> SearchAll(in ReadOnlySpan<byte> searchArray)
        {
            var index = 0;
            var limit = searchArray.Length - _patternLength;
            var patternLengthMinusOne = _patternLength - 1;
            var list = new List<int>();
            fixed (byte* pointerToByteArray = searchArray)
            {
                fixed (byte* pointerToPattern = _pattern)
                {
                    while (index <= limit)
                    {
                        var j = patternLengthMinusOne;
                        while (j >= 0 && pointerToPattern[j] == pointerToByteArray[index + j])
                            j--;
                        if (j < 0)
                            list.Add(index);
                        index += Math.Max(_jumpTable[pointerToByteArray[index + j]] - _patternLength + 1 + j, 1);
                    }
                }
            }
            return list;
        }
        public int SuperSearch(in ReadOnlySpan<byte> searchArray, int nth)
        {
            var e = 0;
            var c = 0;
            do
            {
                e = Search(searchArray.Slice(e));
                if (e == -1)
                    return -1;
                c++;
                e++;
            } while (c < nth);
            return e - 1;
        }
    }
}
