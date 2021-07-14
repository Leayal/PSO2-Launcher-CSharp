using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public class TypedArray
    {
        public readonly Array array;
        public readonly Type type;

        public TypedArray(Type _type, Array _array)
        {
            this.type = _type;
            this.array = _array;
        }
    }
}
