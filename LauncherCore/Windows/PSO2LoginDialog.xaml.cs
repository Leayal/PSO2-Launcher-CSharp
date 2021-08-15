using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Core.UIElements;
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
        private readonly ConfigurationFile config;
        private readonly CancellationTokenSource cancelsrc;

        public PSO2LoginDialog(ConfigurationFile conf, PSO2HttpClient webclient) : this(conf, webclient, null, false) { }

        public PSO2LoginDialog(ConfigurationFile conf, PSO2HttpClient webclient, SecureString username, bool disposeUsername)
        {
            this._loginToken = null;
            this.config = conf;
            this.webclient = webclient;
            this.cancelsrc = new CancellationTokenSource();
            this.cancelsrc.Token.Register(this.cancelsrc.Dispose);

            InitializeComponent();

            var defaultVal = conf.DefaultLoginPasswordRemember;
            EnumComboBox.ValueDOM<LoginPasswordRememberStyle> defaultItem = null;

            var vals = Enum.GetValues<LoginPasswordRememberStyle>();
            var items = new List<EnumComboBox.ValueDOM<LoginPasswordRememberStyle>>(vals.Length);
            for (int i = 0; i < vals.Length; i++)
            {
                var val = vals[i];
                if (!EnumVisibleInOptionAttribute.TryGetIsVisible(val, out var isVisible) || isVisible)
                {
                    if (val == defaultVal)
                    {
                        defaultItem = new EnumComboBox.ValueDOM<LoginPasswordRememberStyle>(val);
                        items.Add(defaultItem);
                    }
                    else
                    {
                        items.Add(new EnumComboBox.ValueDOM<LoginPasswordRememberStyle>(val));
                    }
                }
            }

            this.rememberOption.ItemsSource = items;
            if (defaultItem != null)
            {
                this.rememberOption.SelectedItem = defaultItem;
            }
            else
            {
                this.rememberOption.SelectedIndex = 0;
            }

            if (username != null)
            {
                this.checkbox_rememberusername.IsChecked = true;
                username.Reveal((in ReadOnlySpan<char> chars) =>
                {
                    this.idBox.Text = new string(chars);
                });
                if (disposeUsername)
                {
                    username.Dispose();
                }
            }
        }

        public LoginPasswordRememberStyle SelectedRememberOption
        {
            get
            {
                var selected = this.rememberOption.SelectedItem;
                if (selected is EnumComboBox.ValueDOM<LoginPasswordRememberStyle> dom)
                {
                    return dom.Value;
                }
                else
                {
                    return LoginPasswordRememberStyle.DoNotRemember;
                }
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            var anothertoken = this.cancelsrc.Token;
            anothertoken.Register(() =>
            {
                this.DialogResult = false;
                this.Close();
            });
            try
            {
                if (!this.cancelsrc.IsCancellationRequested)
                {
                    this.cancelsrc.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // Shouldn't be here
            }
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
                        if (id.Length == 0)
                        {
                            this.ShowModalMessageExternal("Notice", "Username field cannot be emptied.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "OK", AnimateHide = false, AnimateShow = false });
                            return;
                        }
                        else if (pw.Length == 0)
                        {
                            this.ShowModalMessageExternal("Notice", "Password field cannot be emptied.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "OK", AnimateHide = false, AnimateShow = false });
                            return;
                        }

                        CancellationToken canceltoken;
                        try
                        {
                            canceltoken = this.cancelsrc.Token;
                        }
                        catch (ObjectDisposedException)
                        {
                            this.DialogResult = false;
                            this.Close();
                            return;
                        }
                        this._loginToken = await this.webclient.LoginPSO2Async(id, pw, canceltoken);
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
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private PSO2LoginToken _loginToken;
        public PSO2LoginToken LoginToken => this._loginToken;

        public SecureString GetPassword()
        {
            var result = this.pwBox.SecurePassword;
            result.MakeReadOnly();
            return result;
        }

        public SecureString GetUsername()
        {
            var str = this.idBox.Text.AsSpan();
            var ss = new SecureString();
            for (int i = 0; i < str.Length; i++)
            {
                ss.AppendChar(str[i]);
            }
            ss.MakeReadOnly();
            return ss;
        }

        public void Dispose()
        {
            this._loginToken = null;
            this.idBox.Text = string.Empty;
            this.pwBox.Password = string.Empty;
        }

        private void RememberOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoginPasswordRememberStyle val;
            var selected = this.rememberOption.SelectedItem;
            if (selected != null)
            {
                if (selected is EnumComboBox.ValueDOM<LoginPasswordRememberStyle> dom)
                {
                    val = dom.Value;
                }
                else
                {
                    val = LoginPasswordRememberStyle.DoNotRemember;
                }
                this.config.DefaultLoginPasswordRemember = val;
                this.config.Save();
            }
        }
    }
}
