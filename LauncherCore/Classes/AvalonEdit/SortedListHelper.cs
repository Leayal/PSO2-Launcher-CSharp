using System;
using System.Collections.Generic;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    static class SortedListHelper
    {
        public static int BinarySearch<T>(this IList<T> list, T value)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            var comp = Comparer<T>.Default;
            int lo = 0, hi = list.Count - 1;
            while (lo < hi)
            {
                int m = MakeAverageSafe(hi, lo);
                if (m == -1)
                {
                    return 0;
                }
                if (comp.Compare(list[m], value) < 0) lo = m + 1;
                else hi = m - 1;
            }
            if (comp.Compare(list[lo], value) < 0) lo++;
            return lo;
        }

        private static int MakeAverageSafe(int hi, int lo)
        {
            long leap = hi + lo;
            var avg = leap / 2;
            if (avg < int.MaxValue)
            {
                return Convert.ToInt32(avg);
            }
            else
            {
                return -1;
            }
        }

        public static int FindFirstIndexGreaterThanOrEqualTo<T, U>(this SortedList<T, U> sortedList, T key) where T : notnull
        {
            return BinarySearch(sortedList.Keys, key);
        }
    }
}
