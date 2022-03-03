using Leayal.Shared.Windows;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for PSO2LoginOtpDialog.xaml
    /// </summary>
    public partial class PSO2LoginOtpDialog : MetroWindowEx
    {
        public SecureString Otp
        {
            get
            {
                var otp = this.OtpBox.SecurePassword;
                if (!otp.IsReadOnly())
                {
                    otp.MakeReadOnly();
                }
                return otp;
            }
        }

        public static readonly DependencyProperty DialogMessageProperty = DependencyProperty.Register("DialogMessage", typeof(string), typeof(PSO2LoginOtpDialog), new PropertyMetadata(string.Empty));

        public string DialogMessage
        {
            get => (string)this.GetValue(DialogMessageProperty);
            set => this.SetValue(DialogMessageProperty, value);
        }

        public PSO2LoginOtpDialog()
        {
            InitializeComponent();
        }

        protected override void OnBeforeShown()
        {
            this.OtpBox.Clear();
            this.OtpBox.Focus();
            base.OnBeforeShown();
        }

        public void ClearPassword() => this.OtpBox.Clear();

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.CustomDialogResult = true;
            this.DialogResult = true;
            this.Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.CustomDialogResult = false;
            this.DialogResult = false;
            this.Close();
        }
    }
}
