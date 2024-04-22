using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    sealed class CategoryAttribute : Attribute
    {
        public CategoryAttribute(string categoryName)
        {
            this.Category = categoryName;
        }

        public string Category { get; }

        public static bool TryGetCategoryName(Type type, object obj, [MaybeNullWhen(false)] out string categoryName)
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
                var attr = Attribute.GetCustomAttribute(mem, typeof(CategoryAttribute)) as CategoryAttribute;
                if (attr != null)
                {
                    categoryName = attr.Category;
                    return true;
                }
            }

            categoryName = default;
            return false;
        }

        public static bool TryGetCategoryName(MemberInfo member, [MaybeNullWhen(false)] out string categoryName)
        {
            // Attribute.GetCustomAttribute()
            if (member != null)
            {
                var attr = Attribute.GetCustomAttribute(member, typeof(CategoryAttribute)) as CategoryAttribute;
                if (attr != null)
                {
                    categoryName = attr.Category;
                    return true;
                }
            }

            categoryName = default;
            return false;
        }
    }
}
