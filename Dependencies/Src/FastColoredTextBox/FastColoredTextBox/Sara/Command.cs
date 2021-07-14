using System.Windows.Forms;

// ReSharper disable once CheckNamespace
namespace FastColoredTextBoxNS
{
    public partial class InsertTextCommand
    {
        internal static void DoEventsSara()
        {
            // I did not include the iteration check here to avoid traversing the stack every time. -Sara

            // This foreach can run for multiple seconds on the MainUI Thread
            // To avoid locking this thread, I have added an Application.DoEvents
            // At 250,000 iterations the MainUI Thread will have been locked for less then 50 ms - Sara
            Application.DoEvents();
        }
    }
}
