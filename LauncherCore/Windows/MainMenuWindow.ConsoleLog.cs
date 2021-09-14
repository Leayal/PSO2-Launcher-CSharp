using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private readonly Classes.CustomHyperlinkElementGenerator consolelog_hyperlinkparser;
        private readonly Dictionary<Guid, ILogDialogFactory> dialogReferenceByUUID;

        public void ShowLogDialogFromGuid(in Guid guid)
        {
            if (this.dialogReferenceByUUID.TryGetValue(guid, out var factory))
            {
                if (factory?.CreateNew() is Window window)
                {
                    window.Owner = this;
                    window.ShowDialog();
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

        interface ILogDialogFactory
        {
            Window CreateNew();
        }

        readonly struct RelativeLogPlacement
        {
            public readonly int Offset, Length;

            public RelativeLogPlacement(in int offset, in int length)
            {
                this.Offset = offset;
                this.Length = length;
            }
        }
    }
}
