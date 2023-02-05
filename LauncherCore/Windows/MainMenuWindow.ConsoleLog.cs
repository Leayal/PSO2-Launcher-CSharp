using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private readonly Classes.CustomHyperlinkElementGenerator consolelog_hyperlinkparser;
        private readonly Dictionary<Guid, ILogDialogHandler> dialogReferenceByUUID;

        public void ShowLogDialogFromGuid(in Guid guid)
        {
            if (this.dialogReferenceByUUID.TryGetValue(guid, out var handler))
            {
                if (handler is ILogDialogFactory factory && factory.CreateNew() is Window window)
                {
                    window.Owner = this;
                    window.ShowDialog();
                }
                else if (handler is ILogDialogDisplayer displayer)
                {
                    displayer.Show();
                }
            }
        }

        private void CreateNewParagraphInLog(string textline, bool newline = true, bool followLastLine = true)
        {
            if (this.ConsoleLog.CheckAccess())
            {
                var consolelog = this.ConsoleLog;
                var doc = consolelog.Document;
                var textlength = doc.TextLength;
                bool isAlreadyInLastLineView = (followLastLine ? ((consolelog.VerticalOffset + consolelog.ViewportHeight) >= (consolelog.ExtentHeight - 1d)) : false);
                doc.BeginUpdate();
                try
                {
                    using (var writer = new ICSharpCode.AvalonEdit.Document.DocumentTextWriter(doc, textlength))
                    {
                        if (newline)
                        {
                            if (textlength != 0)
                            {
                                writer.WriteLine();
                            }
                        }
                        writer.Write(textline);
                        writer.Flush();
                    }
                }
                finally
                {
                    doc.EndUpdate();
                }
                if (followLastLine)
                {
                    consolelog.ScrollToEnd();
                }
            }
            else
            {
                this.ConsoleLog.Dispatcher.BeginInvoke(new Action<string, bool, bool>(this.CreateNewParagraphInLog),
                    new object[] { textline, newline, followLastLine });
            }
        }

        private void CreateNewParagraphInLog(Action<ICSharpCode.AvalonEdit.Document.DocumentTextWriter> callback, bool newline = true, bool followLastLine = true)
        {
            if (this.ConsoleLog.CheckAccess())
            {
                var consolelog = this.ConsoleLog;
                var doc = consolelog.Document;
                var textlength = doc.TextLength;
                bool isAlreadyInLastLineView = (followLastLine ? ((consolelog.VerticalOffset + consolelog.ViewportHeight) >= (consolelog.ExtentHeight - 1d)) : false);
                doc.BeginUpdate();
                try
                {
                    using (var writer = new ICSharpCode.AvalonEdit.Document.DocumentTextWriter(doc, textlength))
                    {
                        if (newline)
                        {
                            if (textlength != 0)
                            {
                                writer.WriteLine();
                            }
                        }
                        callback.Invoke(writer);
                        writer.Flush();
                    }
                }
                finally
                {
                    doc.EndUpdate();
                }
                if (followLastLine)
                {
                    consolelog.ScrollToEnd();
                }
            }
            else
            {
                this.ConsoleLog.Dispatcher.BeginInvoke(new Action<Action<ICSharpCode.AvalonEdit.Document.DocumentTextWriter>, bool, bool>(this.CreateNewParagraphInLog),
                     new object[] { callback, newline, followLastLine });
            }
        }

        private bool CreateErrorLogInLog(string sender, string message, string? title, Exception exception, bool newline = true, bool followLastLine = true)
        {
            var logtext = $"[{sender}] {(string.IsNullOrEmpty(message) ? exception.Message : message)}.";

            var dialogguid = Guid.NewGuid();
            if (Uri.TryCreate(StaticResources.Url_ShowLogDialogFromGuid, dialogguid.ToString(), out var crafted))
            {
                var factory = new ErrorLogDialogFactory(this, message, title, exception);
                this.dialogReferenceByUUID.Add(dialogguid, factory);

                const string showDetail = " (Show details)";
                var urldefines = new Dictionary<RelativeLogPlacement, Uri>(1)
                {
                    { new RelativeLogPlacement(logtext.Length + 1, showDetail.Length - 1), crafted }
                };
                this.CreateNewParagraphFormatHyperlinksInLog(logtext + showDetail, urldefines, newline, followLastLine);
                return true;
            }
            else
            {
                this.CreateNewParagraphInLog(logtext, newline, followLastLine);
                return false;
            }
        }

        private bool CreateErrorLogInLog(string sender, ICollection<System.Windows.Documents.Inline> lines, string? title, Exception exception, bool newline = true, bool followLastLine = true)
        {
            if (lines == null || lines.Count == 0)
            {
                return this.CreateErrorLogInLog(sender, string.Empty, title, exception, newline, followLastLine);
            }

            var firstLine = ((System.Windows.Documents.Run?)lines.FirstOrDefault(x => (x is System.Windows.Documents.Run run && !string.IsNullOrWhiteSpace(run.Text))))?.Text;
            var logtext = $"[{sender}] {(string.IsNullOrEmpty(firstLine) ? exception.Message : firstLine)}.";

            var dialogguid = Guid.NewGuid();
            if (Uri.TryCreate(StaticResources.Url_ShowLogDialogFromGuid, dialogguid.ToString(), out var crafted))
            {
                var factory = new ExtendedErrorLogDialogFactory(this, lines, title, exception);
                this.dialogReferenceByUUID.Add(dialogguid, factory);

                const string showDetail = " (Show details)";
                var urldefines = new Dictionary<RelativeLogPlacement, Uri>(1)
                {
                    { new RelativeLogPlacement(logtext.Length + 1, showDetail.Length - 1), crafted }
                };
                this.CreateNewParagraphFormatHyperlinksInLog(logtext + showDetail, urldefines, newline, followLastLine);
                return true;
            }
            else
            {
                this.CreateNewParagraphInLog(logtext, newline, followLastLine);
                return false;
            }
        }

        private void CreateNewParagraphFormatHyperlinksInLog(string text, IReadOnlyDictionary<RelativeLogPlacement, Uri> urls, bool newline = true, bool followLastLine = true)
        {
            if (urls == null || urls.Count == 0)
            {
                this.CreateNewParagraphInLog(text, newline, followLastLine);
            }
            else
            {
                if (this.ConsoleLog.CheckAccess())
                {
                    var consolelog = this.ConsoleLog;
                    var doc = consolelog.Document;
                    var textlength = doc.TextLength;
                    bool isAlreadyInLastLineView = (followLastLine ? ((consolelog.VerticalOffset + consolelog.ViewportHeight) >= (consolelog.ExtentHeight - 1d)) : false);
                    int writtenoffset;
                    doc.BeginUpdate();
                    try
                    {
                        using (var writer = new ICSharpCode.AvalonEdit.Document.DocumentTextWriter(doc, textlength))
                        {
                            if (newline)
                            {
                                if (textlength != 0)
                                {
                                    writer.WriteLine();
                                }
                            }
                            writtenoffset = writer.InsertionOffset;
                            writer.Write(text);
                            writer.Flush();
                            
                        }
                        // var line = consolelog.Document.GetLineByOffset(writtenoffset);
                        // var relativeoffset = (line.EndOffset - text.Length) - line.Offset;
                        // var lineNumber = line.LineNumber;
                        // var linecontext = consolelog.TextArea.TextView.GetOrConstructVisualLine(line);
                    }
                    finally
                    {
                        doc.EndUpdate();
                    }
                    foreach (var item in urls)
                    {
                        var relativePlacement = item.Key;
                        this.consolelog_hyperlinkparser.Items.Add(
                            new Classes.CustomHyperlinkElementGenerator.Placements(writtenoffset + relativePlacement.Offset, relativePlacement.Length),
                            item.Value);
                    }
                    if (followLastLine)
                    {
                        consolelog.ScrollToEnd();
                    }
                }
                else
                {
                    this.ConsoleLog.Dispatcher.BeginInvoke(new Action<string, IReadOnlyDictionary<RelativeLogPlacement, Uri>, bool, bool>(this.CreateNewParagraphFormatHyperlinksInLog),
                         new object[] { text, urls, newline, followLastLine });
                }
            }
        }

        private static void VisualLineLinkText_LinkClicked(Classes.CustomHyperlinkElementGenerator.VisualLineLinkTextEx element)
        {
            var url = element.NavigateUri;
            if (url != null)
            {
                App.Current?.ExecuteCommandUrl(url);
            }
        }

        private void ConsoleLogMenuItemCopySelected_Click(object sender, RoutedEventArgs e)
        {
            if (this.ConsoleLog.SelectionLength == 0) return;
            Clipboard.SetText(this.ConsoleLog.SelectedText, TextDataFormat.UnicodeText);
        }

        private void ConsoleLogMenuItemCopyAll_Click(object sender, RoutedEventArgs e)
        {
            var str = this.ConsoleLog.Text;
            if (!string.IsNullOrEmpty(str))
            {
                Clipboard.SetText(str, TextDataFormat.UnicodeText);
            }
        }

        private void ConsoleLogMenuItemClearAll_Click(object sender, RoutedEventArgs e)
        {
            this.ConsoleLog.Clear();
            this.ConsoleLog.Document.UndoStack.ClearAll();
            this.consolelog_hyperlinkparser.Items.Clear();
            this.dialogReferenceByUUID.Clear();
        }

        interface ILogDialogHandler { }
        interface ILogDialogFactory : ILogDialogHandler
        {
            Window CreateNew();
        }

        interface ILogDialogDisplayer : ILogDialogHandler
        {
            void Show();
        }

        sealed class ErrorLogDialogFactory : ILogDialogDisplayer
        {
            private readonly Exception exception;
            private readonly string? _message, _title;
            private readonly Window parent;

            public ErrorLogDialogFactory(Window parent, string? message, string? title, Exception exception)
            {
                this.parent = parent;
                this.exception = exception;
                this._message = message;
                this._title = title;
            }

            public void Show() => Prompt_Generic.ShowError(this.parent, this._message, this._title, this.exception);
        }

        sealed class ExtendedErrorLogDialogFactory : ILogDialogDisplayer
        {
            private readonly Exception exception;
            private readonly string? _title;
            private readonly ICollection<System.Windows.Documents.Inline> _lines;
            private readonly Window parent;

            public ExtendedErrorLogDialogFactory(Window parent, ICollection<System.Windows.Documents.Inline> lines, string? title, Exception exception)
            {
                this.parent = parent;
                this.exception = exception;
                this._lines = lines;
                this._title = title;
            }

            public void Show() => Prompt_Generic.ShowError(this.parent, this._lines, this._title, this.exception, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        readonly struct RelativeLogPlacement : IEquatable<RelativeLogPlacement>
        {
            public readonly int Offset, Length;

            public RelativeLogPlacement(in int offset, in int length)
            {
                this.Offset = offset;
                this.Length = length;
            }

            public readonly override bool Equals([NotNullWhen(true)] object? obj) => (obj is RelativeLogPlacement item && this.Equals(item));

            public readonly bool Equals(RelativeLogPlacement item) => (this.Offset == item.Offset && this.Length == item.Length);

            public readonly override int GetHashCode() => HashCode.Combine(this.Offset, this.Length);
        }
    }
}
