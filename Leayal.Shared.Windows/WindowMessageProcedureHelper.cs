using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using global::Windows.Win32;
using global::Windows.Win32.Foundation;

namespace Leayal.Shared.Windows
{
	/// <summary>Convenient methods to deal with Windows Messages, or Windows Procedures.</summary>
    public static class WindowMessageProcedureHelper
    {
        /// <summary>Places (posts) a message in the message queue associated with the thread that created the specified window and returns without waiting for the thread to process the message. (Unicode)</summary>
		/// <param name="hwnd">
		/// <para>A handle to the window whose window procedure is to receive the message. The following values have special meanings. </para>
		/// <para>This doc was truncated.</para>
		/// <para><see href="https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-postmessagew#parameters">Read more on docs.microsoft.com</see>.</para>
		/// </param>
		/// <param name="msg">
		/// <para>The message to be posted. For lists of the system-provided messages, see <a href="https://docs.microsoft.com/windows/desktop/winmsg/about-messages-and-message-queues">System-Defined Messages</a>.</para>
		/// <para><see href="https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-postmessagew#parameters">Read more on docs.microsoft.com</see>.</para>
		/// </param>
		/// <param name="wParam">
		/// <para>Additional message-specific information.</para>
		/// <para><see href="https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-postmessagew#parameters">Read more on docs.microsoft.com</see>.</para>
		/// </param>
		/// <param name="lParam">
		/// <para>Additional message-specific information.</para>
		/// <para><see href="https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-postmessagew#parameters">Read more on docs.microsoft.com</see>.</para>
		/// </param>
		/// <returns>
		/// <para>Type: <b>BOOL</b> If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call <a href="https://docs.microsoft.com/windows/desktop/api/errhandlingapi/nf-errhandlingapi-getlasterror">GetLastError</a>.</para>
		/// </returns>
		/// <remarks>
		/// <para>When a message is blocked by UIPI the last error, retrieved with <a href="https://docs.microsoft.com/windows/desktop/api/errhandlingapi/nf-errhandlingapi-getlasterror">GetLastError</a>, is set to 5 (access denied). Messages in a message queue are retrieved by calls to the <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-getmessage">GetMessage</a> or <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-peekmessagea">PeekMessage</a> function. Applications that need to communicate using <b>HWND_BROADCAST</b> should use the <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-registerwindowmessagea">RegisterWindowMessage</a> function to obtain a unique message for inter-application communication. The system only does marshalling for system messages (those in the range 0 to (<a href="https://docs.microsoft.com/windows/desktop/winmsg/wm-user">WM_USER</a>-1)). To send other messages (those &gt;= <b>WM_USER</b>) to another process, you must do custom marshalling. If you send a message in the range below <a href="https://docs.microsoft.com/windows/desktop/winmsg/wm-user">WM_USER</a> to the asynchronous message functions (<b>PostMessage</b>, <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-sendnotifymessagea">SendNotifyMessage</a>, and <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-sendmessagecallbacka">SendMessageCallback</a>), its message parameters cannot include pointers. Otherwise, the operation will fail. The functions will return before the receiving thread has had a chance to process the message and the sender will free the memory before it is used. Do not post the <a href="https://docs.microsoft.com/windows/desktop/winmsg/wm-quit">WM_QUIT</a> message using <b>PostMessage</b>; use the <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-postquitmessage">PostQuitMessage</a> function. An accessibility application can use <b>PostMessage</b> to post <a href="https://docs.microsoft.com/windows/desktop/inputdev/wm-appcommand">WM_APPCOMMAND</a> messages  to the shell to launch applications. This  functionality is not guaranteed to work for other types of applications. There is a limit of 10,000 posted messages per message queue. This limit should be sufficiently large.  If your application exceeds the limit, it should be redesigned to avoid consuming so many system resources. To adjust this limit, modify the following registry key. <pre><b>HKEY_LOCAL_MACHINE</b> <b>SOFTWARE</b> <b>Microsoft</b> <b>Windows NT</b> <b>CurrentVersion</b> <b>Windows</b> <b>USERPostMessageLimit</b></pre> If the function fails, call <a href="https://docs.microsoft.com/windows/desktop/api/errhandlingapi/nf-errhandlingapi-getlasterror">GetLastError</a> to get extended error information. <b>GetLastError</b> returns <b>ERROR_NOT_ENOUGH_QUOTA</b> when the limit is hit. The minimum acceptable value is 4000.</para>
		/// <para><see href="https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-postmessagew#">Read more on docs.microsoft.com</see>.</para>
		/// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PostWindowMessage(IntPtr hwnd, int msg, nuint wParam, nint lParam)
            => PInvoke.PostMessage(new HWND(hwnd), Convert.ToUInt32(msg), new WPARAM(wParam), new LPARAM(lParam));

        /// <summary>Packs two 16-bit values (2 <seealso cref="ushort"/>s) into one 32-bit value (1 <seealso cref="uint"/>).</summary>
        /// <param name="low">The low-order word value.</param>
        /// <param name="high">The high-order word value.</param>
        /// <returns>A <seealso cref="uint"/> containing both low-order word and high-order word.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint PackUInt_LowHigh_Order(ushort low, ushort high) => (uint)((high << 16) | (low & 0xFFFF));

        /// <summary>Extracts low-order word and high-order word from the 2 32-bit values (2 <seealso cref="int"/>s) and re-pack them into one 32-bit value (1 <seealso cref="int"/>).</summary>
        /// <param name="low">The value to extract low-order word from.</param>
        /// <param name="high">The value to extract high-order word from.</param>
        /// <returns>A <seealso cref="int"/> containing both extracted low-order word and high-order word.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PackInt_LowHigh_Order(int low, int high) => ((high << 16) | (low & 0xFFFF));
    }
}
