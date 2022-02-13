using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.Integration;

namespace Leayal.WebViewCompat
{
    class WindowsFormsHostEx2 : WindowsFormsHost
    {
        private readonly DoubleBufferedPanel panel;
        public WindowsFormsHostEx2() : base()
        {
            this.panel = new DoubleBufferedPanel()
            {
                Location = System.Drawing.Point.Empty,
                AutoScroll = true,
                Dock = System.Windows.Forms.DockStyle.Fill
            };
            base.Child = this.panel;
        }

        public new System.Windows.Forms.Control Child
        {
            get
            {
                if (this.panel.HasChildren)
                {
                    return this.panel.Controls[0];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (this.panel.HasChildren)
                {
                    var control = this.panel.Controls[0];
                    if (object.ReferenceEquals(value, control))
                    {
                        return;
                    }
                    this.panel.Controls.Clear();
                }
                this.panel.Controls.Add(value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                this.panel.Dispose();
            }
        }

        sealed class DoubleBufferedPanel : System.Windows.Forms.TableLayoutPanel
        {
            public DoubleBufferedPanel() : base()
            {
                this.RowCount = 1;
                this.ColumnCount = 1;
                this.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
                this.RowStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
                this.DoubleBuffered = true;
            }
        }
    }
}
