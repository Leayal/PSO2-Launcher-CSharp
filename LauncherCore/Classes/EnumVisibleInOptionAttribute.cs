using System;
using System.Reflection;

namespace Leayal.PSO2Launcher.Core.Classes
{
    public sealed class EnumVisibleInOptionAttribute : Attribute
    {
        public EnumVisibleInOptionAttribute(bool visible)
        {
            this.IsVisible = visible;
        }

        public bool IsVisible { get; }

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

        public static bool TryGetIsVisible(MemberInfo mem, out bool isVisible)
        {
            // Attribute.GetCustomAttribute()
            if (mem != null)
            {
                var attr = Attribute.GetCustomAttribute(mem, typeof(EnumVisibleInOptionAttribute)) as EnumVisibleInOptionAttribute;
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
