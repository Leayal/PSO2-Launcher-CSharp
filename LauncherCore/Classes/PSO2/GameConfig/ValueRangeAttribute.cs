using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig
{
    class ValueRangeAttribute : Attribute
    {
        public ValueRangeAttribute(int min, int max)
        {
            this.Minimum = min;
            this.Maximum = max;
        }

        public virtual int Minimum { get; }
        public virtual int Maximum { get; }

        public static bool TryGetRange(Type type, object obj, out int min, out int max)
        {
            // Attribute.GetCustomAttribute()

            if (!(obj is MemberInfo mem))
            {
                var membername = obj.ToString();
                var mems = type.GetMember(membername);
                if (mems != null && mems.Length != 0)
                {
                    mem = mems[0];
                }
                else
                {
                    mem = null;
                }
            }
            if (mem != null)
            {
                var attr = Attribute.GetCustomAttribute(mem, typeof(ValueRangeAttribute)) as ValueRangeAttribute;
                if (attr != null)
                {
                    min = attr.Minimum;
                    max = attr.Maximum;
                    return true;
                }
            }

            min = default;
            max = int.MaxValue;
            return false;
        }

        public static bool TryGetRange(MemberInfo member, out int min, out int max)
        {
            // Attribute.GetCustomAttribute()
            if (member != null)
            {
                var attr = Attribute.GetCustomAttribute(member, typeof(ValueRangeAttribute)) as ValueRangeAttribute;
                if (attr != null)
                {
                    min = attr.Minimum;
                    max = attr.Maximum;
                    return true;
                }
            }

            min = default;
            max = int.MaxValue;
            return false;
        }
    }
}
