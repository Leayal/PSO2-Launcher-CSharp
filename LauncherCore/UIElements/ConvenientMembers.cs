using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Leayal.PSO2Launcher.Core.UIElements
{
    static class ConvenientMembers
    {
        /// <summary>Identifies extension property "PaddingLeft".</summary>
        /// <remarks>This property allows caller to change only "Left" padding, the rest remains unchanged.</remarks>
        public static readonly DependencyProperty PaddingLeftProperty = DependencyProperty.RegisterAttached("PaddingLeft", typeof(double), typeof(Control), new PropertyMetadata(default(double), (obj, e) =>
        {
            if (obj is Control control)
            {
                var newPaddingLeft = Unsafe.Unbox<double>(e.NewValue);
                var thickness = control.Padding;
                if (thickness.Left != newPaddingLeft)
                {
                    thickness.Left = newPaddingLeft;
                    control.Padding = thickness;
                }
            }
        }, (obj, coerceValue) =>
        {
            if (obj is Control control)
            {
                // var myOwnValue = Unsafe.Unbox<double>(coerceValue);
                return control.Padding.Left;
            }
            return coerceValue;
        }));

        /// <summary>Sets the left padding of a control while leaving other padding values unchanged.</summary>
        /// <param name="control">The control to set the left padding value.</param>
        /// <param name="value">The thickness value.</param>
        public static void SetPaddingLeft(Control control, double value) => control.SetValue(PaddingLeftProperty, value);

        /// <summary>Gets the left padding of a control.</summary>
        /// <param name="control">The control to get the padding value.</param>
        /// <returns>The thickness of left padding.</returns>
        public static double GetPaddingLeft(Control control) => Unsafe.Unbox<double>(control.GetValue(PaddingLeftProperty));

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
