using System;
using System.Collections.Generic;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Insert single char
    /// </summary>
    /// <remarks>This operation includes also insertion of new line and removing char by backspace</remarks>
    public partial class InsertCharCommand : UndoableCommand
    {
        public char c;
        private char deletedChar = '\x0';

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tb">Underlaying textbox</param>
        /// <param name="c">Inserting char</param>
        public InsertCharCommand(TextSource ts, char c) : base(ts)
        {
            this.c = c;
        }

        /// <summary>
        /// Undo operation
        /// </summary>
        public override void Undo()
        {
            Ts.OnTextChanging();
            switch (c)
            {
                case '\n':
                    MergeLines(Sel.Start.iLine, Ts);
                    break;
                case '\r':
                    break;
                case '\b':
                    Ts.CurrentTB.Selection.Start = LastSel.Start;
                    char cc = '\x0';
                    if (deletedChar != '\x0')
                    {
                        Ts.CurrentTB.ExpandBlock(Ts.CurrentTB.Selection.Start.iLine);
                        InsertChar(deletedChar, ref cc, Ts);
                    }
                    break;
                case '\t':
                    Ts.CurrentTB.ExpandBlock(Sel.Start.iLine);
                    for (int i = Sel.FromX; i < LastSel.FromX; i++)
                        Ts[Sel.Start.iLine].RemoveAt(Sel.Start.iChar);
                    Ts.CurrentTB.Selection.Start = Sel.Start;
                    break;
                default:
                    Ts.CurrentTB.ExpandBlock(Sel.Start.iLine);
                    Ts[Sel.Start.iLine].RemoveAt(Sel.Start.iChar);
                    Ts.CurrentTB.Selection.Start = Sel.Start;
                    break;
            }

            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(Sel.Start.iLine, Sel.Start.iLine));

            base.Undo();
        }

        /// <summary>
        /// Execute operation
        /// </summary>
        public override void Execute()
        {
            Ts.CurrentTB.ExpandBlock(Ts.CurrentTB.Selection.Start.iLine);
            string s = c.ToString();
            Ts.OnTextChanging(ref s);
            if (s.Length == 1)
                c = s[0];

            if (string.IsNullOrEmpty(s))
                throw new ArgumentOutOfRangeException();


            if (Ts.Count == 0)
                InsertLine(Ts);
            InsertChar(c, ref deletedChar, Ts);

            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(Ts.CurrentTB.Selection.Start.iLine, Ts.CurrentTB.Selection.Start.iLine));
            base.Execute();
        }

        internal static void InsertChar(char c, ref char deletedChar, TextSource ts)
        {
            var tb = ts.CurrentTB;

            switch (c)
            {
                case '\n':
                    if (!ts.CurrentTB.AllowInsertRemoveLines)
                        throw new ArgumentOutOfRangeException("Cant insert this char in ColumnRange mode");
                    if (ts.Count == 0)
                        InsertLine(ts);
                    InsertLine(ts);
                    break;
                case '\r':
                    break;
                case '\b'://backspace
                    if (tb.Selection.Start.iChar == 0 && tb.Selection.Start.iLine == 0)
                        return;
                    if (tb.Selection.Start.iChar == 0)
                    {
                        if (!ts.CurrentTB.AllowInsertRemoveLines)
                            throw new ArgumentOutOfRangeException("Cant insert this char in ColumnRange mode");
                        if (tb.LineInfos[tb.Selection.Start.iLine - 1].VisibleState != VisibleState.Visible)
                            tb.ExpandBlock(tb.Selection.Start.iLine - 1);
                        deletedChar = '\n';
                        MergeLines(tb.Selection.Start.iLine - 1, ts);
                    }
                    else
                    {
                        deletedChar = ts[tb.Selection.Start.iLine][tb.Selection.Start.iChar - 1].c;
                        ts[tb.Selection.Start.iLine].RemoveAt(tb.Selection.Start.iChar - 1);
                        tb.Selection.Start = new Place(tb.Selection.Start.iChar - 1, tb.Selection.Start.iLine);
                    }
                    break;
                case '\t':
                    int spaceCountNextTabStop = tb.TabLength - (tb.Selection.Start.iChar % tb.TabLength);
                    if (spaceCountNextTabStop == 0)
                        spaceCountNextTabStop = tb.TabLength;

                    for (int i = 0; i < spaceCountNextTabStop; i++)
                        ts[tb.Selection.Start.iLine].Insert(tb.Selection.Start.iChar, new Char(' '));

                    tb.Selection.Start = new Place(tb.Selection.Start.iChar + spaceCountNextTabStop, tb.Selection.Start.iLine);
                    break;
                default:
                    ts[tb.Selection.Start.iLine].Insert(tb.Selection.Start.iChar, new Char(c));
                    tb.Selection.Start = new Place(tb.Selection.Start.iChar + 1, tb.Selection.Start.iLine);
                    break;
            }
        }

        internal static void InsertLine(TextSource ts)
        {
            var tb = ts.CurrentTB;

            if (!tb.Multiline && tb.LinesCount > 0)
                return;

            if (ts.Count == 0)
                ts.InsertLine(0, ts.CreateLine());
            else
                BreakLines(tb.Selection.Start.iLine, tb.Selection.Start.iChar, ts);

            tb.Selection.Start = new Place(0, tb.Selection.Start.iLine + 1);
            ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
        }

        /// <summary>
        /// Merge lines i and i+1
        /// </summary>
        internal static void MergeLines(int i, TextSource ts)
        {
            var tb = ts.CurrentTB;

            if (i + 1 >= ts.Count)
                return;
            tb.ExpandBlock(i);
            tb.ExpandBlock(i + 1);
            int pos = ts[i].Count;
            //
            /*
            if(ts[i].Count == 0)
                ts.RemoveLine(i);
            else*/
            if (ts[i + 1].Count == 0)
                ts.RemoveLine(i + 1);
            else
            {
                ts[i].AddRange(ts[i + 1]);
                ts.RemoveLine(i + 1);
            }
            tb.Selection.Start = new Place(pos, i);
            ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
        }

        internal static void BreakLines(int iLine, int pos, TextSource ts)
        {
            Line newLine = ts.CreateLine();
            for (int i = pos; i < ts[iLine].Count; i++)
                newLine.Add(ts[iLine][i]);
            ts[iLine].RemoveRange(pos, ts[iLine].Count - pos);
            //
            ts.InsertLine(iLine + 1, newLine);
        }

        public override UndoableCommand Clone()
        {
            return new InsertCharCommand(Ts, c);
        }
    }

    /// <summary>
    /// Insert text
    /// </summary>
    public partial class InsertTextCommand : UndoableCommand
    {
        private string? insertedText;

        public string? InsertedText { get => insertedText; set => insertedText = value; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tb">Underlaying textbox</param>
        /// <param name="insertedText">Text for inserting</param>
        public InsertTextCommand(TextSource ts, string insertedText) : base(ts)
        {
            InsertedText = insertedText;
        }

        /// <summary>
        /// Undo operation
        /// </summary>
        public override void Undo()
        {
            Ts.CurrentTB.Selection.Start = Sel.Start;
            Ts.CurrentTB.Selection.End = LastSel.Start;
            Ts.OnTextChanging();
            ClearSelectedCommand.ClearSelected(Ts);
            base.Undo();
        }

        /// <summary>
        /// Execute operation
        /// </summary>
        public override void Execute()
        {
            if (insertedText == null)
                return;

            Ts.OnTextChanging(ref insertedText);
            InsertText(insertedText, Ts);
            base.Execute();
        }

        internal static void InsertText(string insertedText, TextSource ts)
        {
            var tb = ts.CurrentTB;
            try
            {
                tb.Selection.BeginUpdate();
                char cc = '\x0';

                if (ts.Count == 0)
                {
                    InsertCharCommand.InsertLine(ts);
                    tb.Selection.Start = Place.Empty;
                }
                tb.ExpandBlock(tb.Selection.Start.iLine);
                var len = insertedText.Length;
                for (int i = 0; i < len; i++)
                {
                    var c = insertedText[i];
                    if (c == '\r' && (i >= len - 1 || insertedText[i + 1] != '\n'))
                        InsertCharCommand.InsertChar('\n', ref cc, ts);
                    else
                        InsertCharCommand.InsertChar(c, ref cc, ts);
                    if (i % 250000 == 0)
                        DoEventsSara();
                }
                ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
            }
            finally
            {
                tb.Selection.EndUpdate();
            }
        }

        public override UndoableCommand Clone()
        {
            return new InsertTextCommand(Ts, InsertedText!);
        }
    }

    /// <summary>
    /// Insert text into given ranges
    /// </summary>
    public class ReplaceTextCommand : UndoableCommand
    {
        private string insertedText;
        private readonly List<Range> ranges;
        private readonly List<string> prevText = new List<string>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tb">Underlaying textbox</param>
        /// <param name="ranges">List of ranges for replace</param>
        /// <param name="insertedText">Text for inserting</param>
        public ReplaceTextCommand(TextSource ts, List<Range> ranges, string insertedText)
            : base(ts)
        {
            //sort ranges by place
            ranges.Sort((r1, r2) =>
                {
                    if (r1.Start.iLine == r2.Start.iLine)
                        return r1.Start.iChar.CompareTo(r2.Start.iChar);
                    return r1.Start.iLine.CompareTo(r2.Start.iLine);
                });
            //
            this.ranges = ranges;
            this.insertedText = insertedText;
            LastSel = Sel = new RangeInfo(ts.CurrentTB.Selection);
        }

        /// <summary>
        /// Undo operation
        /// </summary>
        public override void Undo()
        {
            var tb = Ts.CurrentTB;

            Ts.OnTextChanging();
            tb.BeginUpdate();

            tb.Selection.BeginUpdate();
            for (int i = 0; i < ranges.Count; i++)
            {
                tb.Selection.Start = ranges[i].Start;
                for (int j = 0; j < insertedText.Length; j++)
                    tb.Selection.GoRight(true);
                ClearSelected(Ts);
                InsertTextCommand.InsertText(prevText[prevText.Count - i - 1], Ts);
            }
            tb.Selection.EndUpdate();
            tb.EndUpdate();

            if (ranges.Count > 0)
                Ts.OnTextChanged(ranges[0].Start.iLine, ranges[^1].End.iLine);

            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
        }

        /// <summary>
        /// Execute operation
        /// </summary>
        public override void Execute()
        {
            var tb = Ts.CurrentTB;
            prevText.Clear();

            Ts.OnTextChanging(ref insertedText);

            tb.Selection.BeginUpdate();
            tb.BeginUpdate();
            for (int i = ranges.Count - 1; i >= 0; i--)
            {
                tb.Selection.Start = ranges[i].Start;
                tb.Selection.End = ranges[i].End;
                prevText.Add(tb.Selection.Text);
                ClearSelected(Ts);
                if (insertedText != "")
                    InsertTextCommand.InsertText(insertedText, Ts);
            }
            if (ranges.Count > 0)
                Ts.OnTextChanged(ranges[0].Start.iLine, ranges[^1].End.iLine);
            tb.EndUpdate();
            tb.Selection.EndUpdate();
            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));

            LastSel = new RangeInfo(tb.Selection);
        }

        public override UndoableCommand Clone()
        {
            return new ReplaceTextCommand(Ts, new List<Range>(ranges), insertedText);
        }

        internal static void ClearSelected(TextSource ts)
        {
            var tb = ts.CurrentTB;

            tb.Selection.Normalize();

            Place start = tb.Selection.Start;
            Place end = tb.Selection.End;
            int fromLine = Math.Min(end.iLine, start.iLine);
            int toLine = Math.Max(end.iLine, start.iLine);
            int fromChar = tb.Selection.FromX;
            int toChar = tb.Selection.ToX;
            if (fromLine < 0)
                return;
            //
            if (fromLine == toLine)
                ts[fromLine].RemoveRange(fromChar, toChar - fromChar);
            else
            {
                ts[fromLine].RemoveRange(fromChar, ts[fromLine].Count - fromChar);
                ts[toLine].RemoveRange(0, toChar);
                ts.RemoveLine(fromLine + 1, toLine - fromLine - 1);
                InsertCharCommand.MergeLines(fromLine, ts);
            }
        }
    }

    /// <summary>
    /// Clear selected text
    /// </summary>
    public class ClearSelectedCommand : UndoableCommand
    {
        private string? deletedText;

        /// <summary>
        /// Construstor
        /// </summary>
        /// <param name="tb">Underlaying textbox</param>
        public ClearSelectedCommand(TextSource ts) : base(ts)
        {
        }

        /// <summary>
        /// Undo operation
        /// </summary>
        public override void Undo()
        {
            Ts.CurrentTB.Selection.Start = new Place(Sel.FromX, Math.Min(Sel.Start.iLine, Sel.End.iLine));
            Ts.OnTextChanging();
            if (deletedText != null)
                InsertTextCommand.InsertText(deletedText, Ts);
            Ts.OnTextChanged(Sel.Start.iLine, Sel.End.iLine);
            Ts.CurrentTB.Selection.Start = Sel.Start;
            Ts.CurrentTB.Selection.End = Sel.End;
        }

        /// <summary>
        /// Execute operation
        /// </summary>
        public override void Execute()
        {
            var tb = Ts.CurrentTB;

            string temp = string.Empty;
            Ts.OnTextChanging(ref temp);
            if (temp == "")
                throw new ArgumentOutOfRangeException();

            deletedText = tb.Selection.Text;
            ClearSelected(Ts);
            LastSel = new RangeInfo(tb.Selection);
            Ts.OnTextChanged(LastSel.Start.iLine, LastSel.Start.iLine);
        }

        internal static void ClearSelected(TextSource ts)
        {
            var tb = ts.CurrentTB;

            Place start = tb.Selection.Start;
            Place end = tb.Selection.End;
            int fromLine = Math.Min(end.iLine, start.iLine);
            int toLine = Math.Max(end.iLine, start.iLine);
            int fromChar = tb.Selection.FromX;
            int toChar = tb.Selection.ToX;
            if (fromLine < 0)
                return;
            //
            if (fromLine == toLine)
                ts[fromLine].RemoveRange(fromChar, toChar - fromChar);
            else
            {
                ts[fromLine].RemoveRange(fromChar, ts[fromLine].Count - fromChar);
                ts[toLine].RemoveRange(0, toChar);
                ts.RemoveLine(fromLine + 1, toLine - fromLine - 1);
                InsertCharCommand.MergeLines(fromLine, ts);
            }
            //
            tb.Selection.Start = new Place(fromChar, fromLine);
            //
            ts.NeedRecalc(new TextSource.TextChangedEventArgs(fromLine, toLine));
        }

        public override UndoableCommand Clone()
        {
            return new ClearSelectedCommand(Ts);
        }
    }

    /// <summary>
    /// Replaces text
    /// </summary>
    public class ReplaceMultipleTextCommand : UndoableCommand
    {
        private readonly List<ReplaceRange> ranges;
        private readonly List<string> prevText = new List<string>();

        public class ReplaceRange
        {
            public Range? ReplacedRange { get; set; }
            public string ReplaceText { get; set; } = string.Empty;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ts">Underlaying textsource</param>
        /// <param name="ranges">List of ranges for replace</param>
        public ReplaceMultipleTextCommand(TextSource ts, List<ReplaceRange> ranges)
            : base(ts)
        {
            //sort ranges by place
            ranges.Sort((r1, r2) =>
            {
                if (r1.ReplacedRange != null && r2.ReplacedRange != null)
                    if (r1.ReplacedRange.Start.iLine == r2.ReplacedRange.Start.iLine)
                        return r1.ReplacedRange.Start.iChar.CompareTo(r2.ReplacedRange.Start.iChar);
                return r1.ReplacedRange!.Start.iLine.CompareTo(r2.ReplacedRange!.Start.iLine);
            });
            //
            this.ranges = ranges;
            LastSel = Sel = new RangeInfo(ts.CurrentTB.Selection);
        }

        /// <summary>
        /// Undo operation
        /// </summary>
        public override void Undo()
        {
            var tb = Ts.CurrentTB;

            Ts.OnTextChanging();

            tb.Selection.BeginUpdate();

            for (int i = 0; i < ranges.Count; i++)
            {
                Range? replacedRange = ranges[i].ReplacedRange;
                if (replacedRange == null)
                    continue;

                tb.Selection.Start = replacedRange.Start;
                for (int j = 0; j < ranges[i].ReplaceText.Length; j++)
                    tb.Selection.GoRight(true);
                ClearSelectedCommand.ClearSelected(Ts);
                var prevTextIndex = ranges.Count - 1 - i;
                InsertTextCommand.InsertText(prevText[prevTextIndex], Ts);
                Ts.OnTextChanged(replacedRange.Start.iLine, replacedRange.Start.iLine);
            }
            tb.Selection.EndUpdate();

            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
        }

        /// <summary>
        /// Execute operation
        /// </summary>
        public override void Execute()
        {
            var tb = Ts.CurrentTB;
            prevText.Clear();

            Ts.OnTextChanging();

            tb.Selection.BeginUpdate();
            for (int i = ranges.Count - 1; i >= 0; i--)
            {
                Range? replacedRange = ranges[i].ReplacedRange;

                if (replacedRange == null)
                    continue;

                tb.Selection.Start = replacedRange.Start;
                tb.Selection.End = replacedRange.End;
                prevText.Add(tb.Selection.Text);
                ClearSelectedCommand.ClearSelected(Ts);
                InsertTextCommand.InsertText(ranges[i].ReplaceText, Ts);
                Ts.OnTextChanged(replacedRange.Start.iLine, replacedRange.End.iLine);
            }
            tb.Selection.EndUpdate();
            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));

            LastSel = new RangeInfo(tb.Selection);
        }

        public override UndoableCommand Clone()
        {
            return new ReplaceMultipleTextCommand(Ts, new List<ReplaceRange>(ranges));
        }
    }

    /// <summary>
    /// Removes lines
    /// </summary>
    public class RemoveLinesCommand : UndoableCommand
    {
        private readonly List<int> iLines;
        private readonly List<string> prevText = new List<string>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tb">Underlaying textbox</param>
        /// <param name="ranges">List of ranges for replace</param>
        /// <param name="insertedText">Text for inserting</param>
        public RemoveLinesCommand(TextSource ts, List<int> iLines)
            : base(ts)
        {
            //sort iLines
            iLines.Sort();
            //
            this.iLines = iLines;
            LastSel = Sel = new RangeInfo(ts.CurrentTB.Selection);
        }

        /// <summary>
        /// Undo operation
        /// </summary>
        public override void Undo()
        {
            var tb = Ts.CurrentTB;

            Ts.OnTextChanging();

            tb.Selection.BeginUpdate();
            //tb.BeginUpdate();
            for (int i = 0; i < iLines.Count; i++)
            {
                var iLine = iLines[i];

                if (iLine < Ts.Count)
                    tb.Selection.Start = new Place(0, iLine);
                else
                    tb.Selection.Start = new Place(Ts[^1].Count, Ts.Count - 1);

                InsertCharCommand.InsertLine(Ts);
                tb.Selection.Start = new Place(0, iLine);
                var text = prevText[prevText.Count - i - 1];
                InsertTextCommand.InsertText(text, Ts);
                Ts[iLine].IsChanged = true;
                if (iLine < Ts.Count - 1)
                    Ts[iLine + 1].IsChanged = true;
                else
                    Ts[iLine - 1].IsChanged = true;
                if (text.Trim() != string.Empty)
                    Ts.OnTextChanged(iLine, iLine);
            }
            //tb.EndUpdate();
            tb.Selection.EndUpdate();

            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
        }

        /// <summary>
        /// Execute operation
        /// </summary>
        public override void Execute()
        {
            var tb = Ts.CurrentTB;
            prevText.Clear();

            Ts.OnTextChanging();

            tb.Selection.BeginUpdate();
            for (int i = iLines.Count - 1; i >= 0; i--)
            {
                var iLine = iLines[i];

                prevText.Add(Ts[iLine].Text);//backward
                Ts.RemoveLine(iLine);
                //ts.OnTextChanged(ranges[i].Start.iLine, ranges[i].End.iLine);
            }
            tb.Selection.Start = new Place(0, 0);
            tb.Selection.EndUpdate();
            Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));

            LastSel = new RangeInfo(tb.Selection);
        }

        public override UndoableCommand Clone()
        {
            return new RemoveLinesCommand(Ts, new List<int>(iLines));
        }
    }

    /// <summary>
    /// Wrapper for multirange commands
    /// </summary>
    public class MultiRangeCommand : UndoableCommand
    {
        private readonly UndoableCommand cmd;
        private readonly Range range;
        private readonly List<UndoableCommand> commandsByRanges = new List<UndoableCommand>();

        public MultiRangeCommand(UndoableCommand command) : base(command.Ts)
        {
            cmd = command;
            range = Ts.CurrentTB.Selection.Clone();
        }

        public override void Execute()
        {
            commandsByRanges.Clear();
            var prevSelection = range.Clone();
            var iChar = -1;
            var iStartLine = prevSelection.Start.iLine;
            var iEndLine = prevSelection.End.iLine;
            Ts.CurrentTB.Selection.ColumnSelectionMode = false;
            Ts.CurrentTB.Selection.BeginUpdate();
            Ts.CurrentTB.BeginUpdate();
            Ts.CurrentTB.AllowInsertRemoveLines = false;
            try
            {
                if (cmd is InsertTextCommand insertTextCommand)
                    ExecuteInsertTextCommand(ref iChar, insertTextCommand.InsertedText!);
                else
                if (cmd is InsertCharCommand insertCharCommand && insertCharCommand.c != '\x0' && insertCharCommand.c != '\b')//if not DEL or BACKSPACE
                    ExecuteInsertTextCommand(ref iChar, insertCharCommand.c.ToString());
                else
                    ExecuteCommand(ref iChar);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            finally
            {
                Ts.CurrentTB.AllowInsertRemoveLines = true;
                Ts.CurrentTB.EndUpdate();

                Ts.CurrentTB.Selection = range;
                if (iChar >= 0)
                {
                    Ts.CurrentTB.Selection.Start = new Place(iChar, iStartLine);
                    Ts.CurrentTB.Selection.End = new Place(iChar, iEndLine);
                }
                Ts.CurrentTB.Selection.ColumnSelectionMode = true;
                Ts.CurrentTB.Selection.EndUpdate();
            }
        }

        private void ExecuteInsertTextCommand(ref int iChar, string text)
        {
            var lines = text.Split('\n');
            var iLine = 0;
            foreach (var r in range.GetSubRanges(true))
            {
                var line = Ts.CurrentTB[r.Start.iLine];
                var lineIsEmpty = r.End < r.Start && line.StartSpacesCount == line.Count;
                if (!lineIsEmpty)
                {
                    var insertedText = lines[iLine % lines.Length];
                    if (r.End < r.Start && insertedText != "")
                    {
                        //add forwarding spaces
                        insertedText = new string(' ', r.Start.iChar - r.End.iChar) + insertedText;
                        r.Start = r.End;
                    }
                    Ts.CurrentTB.Selection = r;
                    var c = new InsertTextCommand(Ts, insertedText);
                    c.Execute();
                    if (Ts.CurrentTB.Selection.End.iChar > iChar)
                        iChar = Ts.CurrentTB.Selection.End.iChar;
                    commandsByRanges.Add(c);
                }
                iLine++;
            }
        }

        private void ExecuteCommand(ref int iChar)
        {
            foreach (var r in range.GetSubRanges(false))
            {
                Ts.CurrentTB.Selection = r;
                var c = cmd.Clone();
                c.Execute();
                if (Ts.CurrentTB.Selection.End.iChar > iChar)
                    iChar = Ts.CurrentTB.Selection.End.iChar;
                commandsByRanges.Add(c);
            }
        }

        public override void Undo()
        {
            Ts.CurrentTB.BeginUpdate();
            Ts.CurrentTB.Selection.BeginUpdate();
            try
            {
                for (int i = commandsByRanges.Count - 1; i >= 0; i--)
                    commandsByRanges[i].Undo();
            }
            finally
            {
                Ts.CurrentTB.Selection.EndUpdate();
                Ts.CurrentTB.EndUpdate();
            }
            Ts.CurrentTB.Selection = range.Clone();
            Ts.CurrentTB.OnTextChanged(range);
            Ts.CurrentTB.OnSelectionChanged();
            Ts.CurrentTB.Selection.ColumnSelectionMode = true;
        }

        public override UndoableCommand Clone()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Remembers current selection and restore it after Undo
    /// </summary>
    public class SelectCommand : UndoableCommand
    {
        public SelectCommand(TextSource ts) : base(ts)
        {
        }

        public override void Execute()
        {
            //remember selection
            LastSel = new RangeInfo(Ts.CurrentTB.Selection);
        }

        protected override void OnTextChanged(bool invert)
        {
        }

        public override void Undo()
        {
            //restore selection
            Ts.CurrentTB.Selection = new Range(Ts.CurrentTB, LastSel.Start, LastSel.End);
        }

        public override UndoableCommand Clone()
        {
            var result = new SelectCommand(Ts);
            if (LastSel != null)
                result.LastSel = new RangeInfo(new Range(Ts.CurrentTB, LastSel.Start, LastSel.End));
            return result;
        }
    }
}
