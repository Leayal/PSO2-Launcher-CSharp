using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Leayal.PSO2Launcher.RSS
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SupportUriHostAttribute : Attribute
    {
        private readonly string _host;

        public SupportUriHostAttribute(string host) : base()
        {
            this._host = host;
        }

        public static bool TryGet(Type t, out string host)
        {
            var attr = Attribute.GetCustomAttribute(t, typeof(SupportUriHostAttribute));
            if (attr is SupportUriHostAttribute supporthost)
            {
                host = supporthost._host;
                return true;
            }
            else
            {
                host = default;
                return false;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SupportUriRegexAttribute : Attribute
    {
        private readonly Regex _regex;

        public SupportUriRegexAttribute(string regex, RegexOptions options) : base()
        {
            this._regex = new Regex(regex, options | RegexOptions.Compiled);
        }

        public static bool TryGet(Type t, out Regex host)
        {
            var attr = Attribute.GetCustomAttribute(t, typeof(SupportUriRegexAttribute));
            if (attr is SupportUriRegexAttribute supporthost)
            {
                host = supporthost._regex;
                return true;
            }
            else
            {
                host = default;
                return false;
            }
        }
    }
}
