using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FastColoredTextBoxNS
{
    public class SyntaxDescriptor : IDisposable
    {
        public char leftBracket = '(';
        public char rightBracket = ')';
        public char leftBracket2 = '{';
        public char rightBracket2 = '}';
        public BracketsHighlightStrategy bracketsHighlightStrategy = BracketsHighlightStrategy.Strategy2;
        public readonly List<Style> styles = new List<Style>();
        public readonly List<RuleDesc> rules = new List<RuleDesc>();
        public readonly List<FoldingDesc> foldings = new List<FoldingDesc>();

        public void Dispose()
        {
            foreach (var style in styles)
                style.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class RuleDesc
    {
        private Regex? regex;
        private string? pattern;
        private RegexOptions options = RegexOptions.None;
        private Style? style;

        public Regex Regex
        {
            get
            {
                if (regex == null)
                    regex = new Regex(Pattern!, SyntaxHighlighter.RegexCompiledOption | Options);
                return regex;
            }
        }

        public string? Pattern { get => pattern; set => pattern = value; }
        public RegexOptions Options { get => options; set => options = value; }
        public Style? Style { get => style; set => style = value; }
    }

    public class FoldingDesc
    {
        private string? startMarkerRegex;
        private string? finishMarkerRegex;
        private RegexOptions options = RegexOptions.None;

        public string? StartMarkerRegex { get => startMarkerRegex; set => startMarkerRegex = value; }
        public string? FinishMarkerRegex { get => finishMarkerRegex; set => finishMarkerRegex = value; }
        public RegexOptions Options { get => options; set => options = value; }
    }
}
