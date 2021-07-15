using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.Shared
{
    public static class NumericHelper
    {
        public static long ToInt64(this bool value) => value switch { false => 0L, _ => 1L };

        public static int ToInt32(this bool value) => value switch { false => 0, _ => 1 };
    }
}
