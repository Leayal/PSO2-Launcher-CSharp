using ICSharpCode.AvalonEdit.Rendering;
using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Media;

namespace Leayal.PSO2Launcher.Core.Classes.AvalonEdit
{
    class TextStaticTransformData : TextTransformData
    {
        public Typeface? Typeface;
        public Brush? TextColor_DarkTheme, TextColor_LightTheme;
        public TextDecorationCollection? TextDecoration;
        public TextEffectCollection? TextEffects;

        public TextStaticTransformData(int absoluteOffset, int length, Brush? textColor_darkTheme, Brush? textColor_lightTheme) : this(new Placement(in absoluteOffset, in length), null, textColor_darkTheme, textColor_lightTheme) { }

        public TextStaticTransformData(int absoluteOffset, int length, Typeface? typeface) : this(new Placement(in absoluteOffset, in length), typeface, null, null) { }

        public TextStaticTransformData(int absoluteOffset, int length, Typeface? typeface, Brush? textColor_darkTheme, Brush? textColor_lightTheme) : this(new Placement(in absoluteOffset, in length), typeface, textColor_darkTheme, textColor_lightTheme) { }

        public TextStaticTransformData(in Placement placement, Typeface? typeface, Brush? textColor_darkTheme, Brush? textColor_lightTheme) : base(in placement)
        {
            this.Typeface = typeface;
            this.TextColor_DarkTheme = textColor_darkTheme;
            this.TextColor_LightTheme = textColor_lightTheme;
        }

        public override Action<VisualLineElement> ApplyChanges => this.InternalApplyChanges;

        private void InternalApplyChanges(VisualLineElement element)
        {
            var props = element.TextRunProperties;

            if (App.Current.IsLightMode)
            {
                if (this.TextColor_LightTheme != null && !props.ForegroundBrush.IsEqualTo(this.TextColor_LightTheme))
                {
                    props.SetForegroundBrush(this.TextColor_LightTheme);
                }
            }
            else
            {
                if (this.TextColor_DarkTheme != null && !props.ForegroundBrush.IsEqualTo(this.TextColor_DarkTheme))
                {
                    props.SetForegroundBrush(this.TextColor_DarkTheme);
                }
            }
            if (this.Typeface != null)
            {
                var typeface = props.Typeface;
                if (!typeface.Equals(this.Typeface))
                {
                    props.SetTypeface(this.Typeface);
                }
            }
            if (this.TextDecoration != null)
            {
                props.SetTextDecorations(this.TextDecoration);
            }
            if (this.TextEffects != null)
            {
                props.SetTextEffects(this.TextEffects);
            }
        }
    }
}
