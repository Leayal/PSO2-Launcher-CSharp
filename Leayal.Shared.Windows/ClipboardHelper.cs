using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Leayal.Shared.Windows
{
    /// <summary>A shorthand class to deal with Clipboard</summary>
    public static class ClipboardHelper
    {
        // In case "DragDropEffects.Move" doesn't exist, try to using number "2" directly.
        private const int DragDropEffects_Move = (int)DragDropEffects.Move;
        private const int CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0);

        /// <summary></summary>
        /// <param name="data"></param>
        /// <param name="retryTimes"></param>
        /// <exception cref="ExternalException"></exception>
        private static void SetClipboardData(DataObject data, int retryTimes)
        {
            if (retryTimes == 0)
            {
                try
                {
                    Clipboard.SetDataObject(data, true);
                }
                catch (ExternalException ex) when (ex.ErrorCode == CLIPBRD_E_CANT_OPEN)
                {
                    // Silent it
                }
            }
            else
            {
                int lastTry = retryTimes - 1;
                for (int i = 0; i < retryTimes; i++)
                {
                    try
                    {
                        Clipboard.SetDataObject(data, true);
                    }
                    catch (ExternalException ex) when (ex.ErrorCode == CLIPBRD_E_CANT_OPEN && i < lastTry)
                    {
                        // Silent it
                    }
                }
            }
        }

        /// <summary>Simulates a cut/copy file operation just like File Explorer.</summary>
        /// <param name="files">The list of files for the operation.</param>
        /// <param name="isFileCut">Determine is cut or copy operation. True for FileCut, False for FileCopy.</param>
        /// <param name="retryTimes">The number of retries before throwing error stating that the clipboard can't be accessed. Set to 0 to disable retry and silent the error.</param>
        /// <exception cref="InvalidOperationException">The calling thread is not <seealso cref="ApartmentState.STA"/>.</exception>
        public static void PutFilesToClipboard(StringCollection files, bool isFileCut, int retryTimes = 5)
        {
            ArgumentNullException.ThrowIfNull(files);
            ArgumentOutOfRangeException.ThrowIfLessThan(retryTimes, 0);
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) throw new InvalidOperationException("The thread calling this method must be STA (Single Thread Apartment)");

            DataObject data = new DataObject();
            data.SetFileDropList(files);
            if (isFileCut)
            {
                data.SetData("Preferred DropEffect", DragDropEffects_Move);
            }
            SetClipboardData(data, retryTimes);
        }

        /// <summary>Copies a string to clipboard.</summary>
        /// <param name="str">The string to be copied to clipboard.</param>
        /// <param name="retryTimes">The number of retries before throwing error stating that the clipboard can't be accessed. Set to 0 to disable retry and silent the error.</param>
        /// <exception cref="InvalidOperationException">The calling thread is not <seealso cref="ApartmentState.STA"/>.</exception>
        public static void PutStringToClipboard(string str, int retryTimes = 5)
        {
            ArgumentNullException.ThrowIfNull(str);
            ArgumentOutOfRangeException.ThrowIfLessThan(retryTimes, 0);
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) throw new InvalidOperationException("The thread calling this method must be STA (Single Thread Apartment)");

            var data = new DataObject();
            data.SetText(str);
            SetClipboardData(data, retryTimes);
        }
    }
}
