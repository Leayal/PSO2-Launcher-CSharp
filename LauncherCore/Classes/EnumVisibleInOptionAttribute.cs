using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public class EnumVisibleInOptionAttribute : Attribute
    {
        public EnumVisibleInOptionAttribute(bool visible)
        {
            this.IsVisible = visible;
        }

        public virtual bool IsVisible { get; }

        public static bool TryGetIsVisible(Enum obj, out bool isVisible)
        {
            // Attribute.GetCustomAttribute()
            var membername = obj.ToString();
            var type = obj.GetType();
            var mem = type.GetMember(membername);
            if (mem != null && mem.Length != 0)
            {
                var attr = Attribute.GetCustomAttribute(mem[0], typeof(EnumVisibleInOptionAttribute)) as EnumVisibleInOptionAttribute;
                if (attr != null)
                {
                    isVisible = attr.IsVisible;
                    return true;
                }
            }

            isVisible = default;
            return false;
        }
    }
}
