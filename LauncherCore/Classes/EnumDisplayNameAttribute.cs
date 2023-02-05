using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class EnumDisplayNameAttribute : Attribute
    {
        public EnumDisplayNameAttribute(string displayName)
        {
            this.DisplayName = displayName;
        }

        public virtual string DisplayName { get; }

        public static bool TryGetDisplayName(Enum obj, [MaybeNullWhen(false)] out string displayName)
        {
            // Attribute.GetCustomAttribute()
            var membername = obj.ToString();
            var type = obj.GetType();
            var mem = type.GetMember(membername);
            if (mem != null && mem.Length != 0)
            {
                var attr = Attribute.GetCustomAttribute(mem[0], typeof(EnumDisplayNameAttribute)) as EnumDisplayNameAttribute;
                if (attr != null)
                {
                    displayName = attr.DisplayName;
                    return true;
                }
            }

            displayName = default;
            return false;
        }

        public static bool TryGetDisplayName(Type type, object obj, [MaybeNullWhen(false)] out string displayName)
        {
            // Attribute.GetCustomAttribute()
            var mem = obj as MemberInfo;
            if (mem == null)
            {
                var membername = obj.ToString();
                if (string.IsNullOrEmpty(membername))
                {
                    mem = null;
                }
                else
                {
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
            }
            if (mem != null)
            {
                var attr = Attribute.GetCustomAttribute(mem, typeof(EnumDisplayNameAttribute)) as EnumDisplayNameAttribute;
                if (attr != null)
                {
                    displayName = attr.DisplayName;
                    return true;
                }
            }

            displayName = default;
            return false;
        }

        public static bool TryGetDisplayName(MemberInfo member, [MaybeNullWhen(false)] out string displayName)
        {
            // Attribute.GetCustomAttribute()
            if (member != null)
            {
                var attr = Attribute.GetCustomAttribute(member, typeof(EnumDisplayNameAttribute)) as EnumDisplayNameAttribute;
                if (attr != null)
                {
                    displayName = attr.DisplayName;
                    return true;
                }
            }

            displayName = default;
            return false;
        }
    }
}
