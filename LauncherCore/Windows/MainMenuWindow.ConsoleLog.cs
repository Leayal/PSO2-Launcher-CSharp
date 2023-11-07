using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using Leayal.PSO2Launcher.Core.Classes.AvalonEdit;
using Leayal.Shared.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private readonly CustomElementGenerator consolelog_hyperlinkparser;
        private readonly CustomColorTextTransformer consolelog_textcolorizer;
        private readonly Dictionary<Guid, ILogDialogHandler> dialogReferenceByUUID;
        private readonly Typeface consolelog_boldTypeface;

        delegate void ConsoleLogWriter<TArg>(TextEditor consoleLog, DocumentTextWriter writer, int absoluteOffsetOfDocumentLine, TArg arg);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        /// <summary>Only be here for convenient edits.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConsoleLogHelper_WriteLogSender(DocumentTextWriter writer, string sender)
        {
            var absoluteOffsetOfItem = writer.InsertionOffset;
            writer.Write('[');
            writer.Write(sender);
            writer.Write(']');
            this.consolelog_textcolorizer.Add(new TextStaticTransformData(absoluteOffsetOfItem, sender.Length + 2, this.consolelog_boldTypeface, Brushes.LightGreen, Brushes.DarkGreen));
        }

        /// <summary>Only be here for convenient edits.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConsoleLogHelper_WriteHyperLink(DocumentTextWriter writer, string caption, Uri url, Action<HyperlinkVisualLineElementData>? linkClickedCallback)
        {
            var absoluteOffsetOfItem = writer.InsertionOffset;
            writer.Write(caption);

            /*
            var hyperlinkTextTransform = new TextCustomTransformData(absoluteOffsetOfItem, caption.Length, (transformData, element) =>
            {
                var props = element.TextRunProperties;
                if (App.Current.IsLightMode)
                {
                    props.SetForegroundBrush(Brushes.DarkBlue);
                }
                else
                {
                    props.SetForegroundBrush(Brushes.LightBlue);
                }
                props.SetTextDecorations(TextDecorations.Underline);
                if (transformData.Properties.TryGetValue("custom_typeface", out var val) && val is Typeface typeface)
                {
                    props.SetTypeface(typeface);
                }
            });
            hyperlinkTextTransform.Properties.Add("custom_typeface", this.consolelog_boldTypeface);
            */
            // The whole thing above is the same as below, just longer and more customizable.
            var hyperlinkTextTransform = new TextStaticTransformData(absoluteOffsetOfItem, caption.Length, this.consolelog_boldTypeface, Brushes.LightBlue, Brushes.DarkBlue)
            {
                TextDecoration = TextDecorations.Underline
            };
            this.consolelog_textcolorizer.Add(hyperlinkTextTransform);
            var hyperlinkVisualLineElementData = new HyperlinkVisualLineElementData(absoluteOffsetOfItem, caption, url);
            if (linkClickedCallback != null)
            {
                hyperlinkVisualLineElementData.LinkClicked += linkClickedCallback;
            }
            this.consolelog_hyperlinkparser.Add(hyperlinkVisualLineElementData);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void CreateNewLineInConsoleLog<TArg>(string? sender, ConsoleLogWriter<TArg> callback, TArg arg, bool newline = true, bool followLastLine = true)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (this.ConsoleLog.CheckAccess())
            {
                var consolelog = this.ConsoleLog;
                var doc = consolelog.Document;

                doc.BeginUpdate();
                var textlength = doc.TextLength;
                bool isAlreadyInLastLineView = (followLastLine ? ((consolelog.VerticalOffset + consolelog.ViewportHeight) >= (consolelog.ExtentHeight - 1d)) : false);
                
                try
                {
                    using (var writer = new DocumentTextWriter(doc, textlength))
                    {
                        if (newline && textlength != 0)
                        {
                            writer.WriteLine();
                        }
                        if (!string.IsNullOrEmpty(sender))
                        {
                            ConsoleLogHelper_WriteLogSender(writer, sender);
                            writer.Write(' ');
                        }
                        callback.Invoke(consolelog, writer, writer.InsertionOffset, arg);
                        writer.Flush();
                    }
                }
                finally
                {
                    doc.EndUpdate();
                }
                if (isAlreadyInLastLineView && followLastLine)
                {
                    consolelog.ScrollToEnd();
                }
            }
            else
            {
                this.ConsoleLog.Dispatcher.BeginInvoke(new Action<string, ConsoleLogWriter<TArg>, TArg, bool, bool>(this.CreateNewLineInConsoleLog),
                     new object?[] { sender, callback, arg, newline, followLastLine });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateNewLineInConsoleLog(string sender, string message, Brush? textColor_darkTheme = null, Brush? textColor_lightTheme = null, bool newline = true, bool followLastLine = true)
        {
            this.CreateNewLineInConsoleLog(sender, (console, writer, absoluteOffsetOfCurrentLine, arg) =>
            {
                var (myself, sender, message) = arg;
                writer.Write(message);
                if (message.AsSpan()[message.Length - 1] != '.')
                {
                    writer.Write('.');
                }
            }, (this, sender, message), newline, followLastLine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateNewErrorLineInConsoleLog(string sender, string message, string? title, Exception exception, bool newline = true, bool followLastLine = true)
        {
            this.CreateNewLineInConsoleLog(sender, (console, writer, absoluteOffsetOfCurrentLine, arg) =>
            {
                var (myself, sender, message, title, exception) = arg;

                var absoluteOffsetStart = writer.InsertionOffset;
                writer.Write("{ERROR} ");
                var logtext = (string.IsNullOrEmpty(message) ? exception.Message : message);
                writer.Write(logtext);
                if (logtext.AsSpan()[logtext.Length - 1] != '.')
                {
                    writer.Write('.');
                }
                var absoluteOffsetEnd = writer.InsertionOffset;
                myself.consolelog_textcolorizer.Add(new TextStaticTransformData(absoluteOffsetStart, absoluteOffsetEnd - absoluteOffsetStart, myself.consolelog_boldTypeface, Brushes.IndianRed, Brushes.DarkRed));

                var dialogguid = Guid.NewGuid();
                if (Uri.TryCreate(StaticResources.Url_ShowLogDialogFromGuid, dialogguid.ToString(), out var crafted))
                {
                    var factory = new ErrorLogDialogFactory(myself, message, title, exception);
                    myself.dialogReferenceByUUID.Add(dialogguid, factory);
                    writer.Write(' ');
                    myself.ConsoleLogHelper_WriteHyperLink(writer, "(Show details)", crafted, VisualLineLinkText_LinkClicked);
                }
            }, (this, sender, message, title, exception), newline, followLastLine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateNewWarnLineInConsoleLog(string sender, string message, bool newline = true, bool followLastLine = true)
        {
            this.CreateNewLineInConsoleLog(sender, (console, writer, absoluteOffsetOfCurrentLine, arg) =>
            {
                var (myself, sender, message) = arg;

                var absoluteOffsetStart = writer.InsertionOffset;
                writer.Write("{WARN} ");
                writer.Write(message);
                var absoluteOffsetEnd = writer.InsertionOffset;
                myself.consolelog_textcolorizer.Add(new TextStaticTransformData(absoluteOffsetStart, absoluteOffsetEnd - absoluteOffsetStart, Brushes.Gold, Brushes.DarkGoldenrod)
                {
                    Typeface = this.consolelog_boldTypeface
                });
            }, (this, sender, message), newline, followLastLine);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void CreateNewErrorLineInConsoleLog(string sender, ICollection<System.Windows.Documents.Inline> lines, string? title, Exception exception, bool newline = true, bool followLastLine = true)
        {
            if (lines == null || lines.Count == 0)
            {
                this.CreateNewErrorLineInConsoleLog(sender, string.Empty, title, exception, newline, followLastLine);
                return;
            }

            var firstLine = ((System.Windows.Documents.Run?)lines.FirstOrDefault(x => (x is System.Windows.Documents.Run run && !string.IsNullOrWhiteSpace(run.Text))))?.Text;
            this.CreateNewErrorLineInConsoleLog(sender, string.IsNullOrEmpty(firstLine) ? exception.Message : firstLine, title, exception, newline, followLastLine);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void VisualLineLinkText_LinkClicked(HyperlinkVisualLineElementData element)
        {
            var url = element.NavigateUri;
            if (url != null)
            {
                App.Current?.ExecuteCommandUrl(url);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void ConsoleLog_OpenLinkWithDefaultBrowser(HyperlinkVisualLineElementData element)
        {
            var url = element.NavigateUri;
            if (url != null)
            {
                try
                {
                    WindowsExplorerHelper.OpenUrlWithDefaultBrowser(url);
                }
                catch { }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void ConsoleLog_SelectLocalPathLinkInExplorer(HyperlinkVisualLineElementData element)
        {
            var url = element.NavigateUri;
            if (url != null && url.IsFile)
            {
                try
                {
                    WindowsExplorerHelper.SelectPathInExplorer(url.LocalPath);
                }
                catch { }
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
            this.consolelog_hyperlinkparser.Clear();
            this.consolelog_textcolorizer.Clear();
            this.ConsoleLog.Clear();
            this.ConsoleLog.Document.UndoStack.ClearAll();
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
