using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for PSO2LoginDialog.xaml
    /// </summary>
    public partial class PSO2LoginDialog : MetroWindowEx, IDisposable
    {
        private readonly PSO2HttpClient webclient;

        public PSO2LoginDialog(PSO2HttpClient webclient)
        {
            this._loginToken = null;
            this.webclient = webclient;
            InitializeComponent();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private async void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.IsEnabled = false;
                try
                {
                    using (var id = this.GetUsername())
                    using (var pw = this.GetPassword())
                    {
                        this._loginToken = await this.webclient.LoginPSO2Async(id, pw, CancellationToken.None);
                    }
                    this.DialogResult = true;
                    this.Close();
                }
                catch (PSO2LoginException ex)
                {
                    await this.ShowMessageAsync("Login failure", "Failed to login.\r\nError code: " + ex.ErrorCode.ToString(), MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "OK", AnimateHide = true, AnimateShow = false });
                }
                catch (UnexpectedDataFormatException ex)
                {
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private PSO2LoginToken _loginToken;
        public PSO2LoginToken LoginToken => this._loginToken;

        private SecureString GetPassword() => this.pwBox.SecurePassword;

        private SecureString GetUsername()
        {
            var str = this.idBox.Text.ToArray();
            var ss = new SecureString();
            for (int i = 0; i < str.Length; i++)
            {
                ss.AppendChar(str[i]);
                str[i] = '\0';
            }

            return ss;
        }

        public void Dispose()
        {
            this._loginToken = null;
            this.idBox.Text = string.Empty;
            this.pwBox.Password = string.Empty;
        }
    }
}
