using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    static class ConvenientEventHandlers
    {
        /// <summary>When this method is added to a <seealso cref="System.Windows.Controls.Primitives.Selector.SelectionChanged"/>, prevent the control from selecting no items.</summary>
        /// <remarks>
        /// <para>You can also just pass-through the event handler to this handler.</para>
        /// <para>Currently supports only <seealso cref="TabControl"/>.</para>
        /// </remarks>
        /// <param name="sender">The element which raised this event. May be <see langword="null"/>.</param>
        /// <param name="e">The event parameters which associated with the event.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TabControl_SelectionChanged_PreventSelectingNothing(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count == 0)
            {
                if (e.RemovedItems[0] is TabItem tab)
                {
                    e.Handled = true;
                    tab.IsSelected = true;
                }
            }
        }
    }
}
