using System;
using System.Collections.Specialized;
using System.Threading;
using System.Windows;

namespace Leayal.Shared.Windows
{
    /// <summary>A shorthand class to deal with Clipboard</summary>
    public static class ClipboardHelper
    {
        /// <summary>Simulates a cut/copy file operation just like File Explorer.</summary>
        /// <param name="files">The list of files for the operation.</param>
        /// <param name="isFileCut">Determine is cut or copy operation. True for FileCut, False for FileCopy.</param>
        /// <exception cref="InvalidOperationException">The calling thread is not <seealso cref="ApartmentState.STA"/>.</exception>
        public static void PutFilesToClipboard(StringCollection files, bool isFileCut)
        {
            var state = Thread.CurrentThread.GetApartmentState();
            if (state != ApartmentState.STA) throw new InvalidOperationException("The thread calling this method must be STA (Single Thread Apartment)");
            DataObject data = new DataObject();
            data.SetFileDropList(files);
            if (isFileCut)
            {
                // In case "DragDropEffects.Move" doesn't work, try to use "2".
                data.SetData("Preferred DropEffect", (int)DragDropEffects.Move);
            }
            Clipboard.SetDataObject(data, true);
        }
    }
}
