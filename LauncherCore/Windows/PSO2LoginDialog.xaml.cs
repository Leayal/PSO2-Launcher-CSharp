using Leayal.PSO2Launcher.Core.Classes;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes;
using Leayal.PSO2Launcher.Core.UIElements;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Leayal.Shared.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    /// <summary>
    /// Interaction logic for PSO2LoginDialog.xaml
    /// </summary>
    public partial class PSO2LoginDialog : MetroWindowEx, IDisposable
    {
        public const int OTP_RetryTimes_Safe = 5;
        // private readonly static MetroDialogSettings _otpDialogSetting = new MetroDialogSettings() { AnimateHide = false, AnimateShow = false, OwnerCanCloseWithDialog = true };
        private static readonly LoginDialogSettings _otpDialogSetting = new LoginDialogSettings() { RememberCheckBoxVisibility = Visibility.Collapsed, ShouldHideUsername = true, EnablePasswordPreview = true };
        public readonly static DependencyProperty IsInLoadingProperty = DependencyProperty.Register("IsInLoading", typeof(bool), typeof(PSO2LoginDialog), new PropertyMetadata(false));
        public bool IsInLoading
        {
            get => (bool)this.GetValue(IsInLoadingProperty);
            set => this.SetValue(IsInLoadingProperty, value);
        }

        public readonly static DependencyProperty ShowOtpFieldProperty = DependencyProperty.Register("ShowOtpField", typeof(bool), typeof(PSO2LoginDialog), new PropertyMetadata(false));
        public bool ShowOtpField
        {
            get => (bool)this.GetValue(ShowOtpFieldProperty);
            set => this.SetValue(ShowOtpFieldProperty, value);
        }

        private readonly PSO2HttpClient webclient;
        private readonly ConfigurationFile config;
        private readonly CancellationTokenSource cancelsrc;

        public PSO2LoginDialog(ConfigurationFile conf, PSO2HttpClient webclient) : this(conf, webclient, null, false) { }

        public PSO2LoginDialog(ConfigurationFile conf, PSO2HttpClient webclient, SecureString? username, bool disposeUsername)
        {
            this._loginToken = null;
            this.config = conf;
            this.webclient = webclient;
            this.cancelsrc = new CancellationTokenSource();
            this.cancelsrc.Token.Register(this.cancelsrc.Dispose);

            InitializeComponent();

            var defaultVal = conf.DefaultLoginPasswordRemember;
            EnumComboBox.ValueDOM<LoginPasswordRememberStyle>? defaultItem = null;

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
            anothertoken.Register(this.CancelLoginInput);
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

        private void CancelLoginInput()
        {
            this.CustomDialogResult = false;
            this.Close();
        }

        private async void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.IsEnabled = false;
                this.IsInLoading = true;
                try
                {
                    using (var id = this.GetUsername())
                    using (var pw = this.GetPassword())
                    {
                        if (id.Length == 0)
                        {
                            this.ShowModalMessageExternal("Notice", "Username field cannot be emptied.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "OK", AnimateHide = false, AnimateShow = false });
                        }
                        else if (pw.Length == 0)
                        {
                            this.ShowModalMessageExternal("Notice", "Password field cannot be emptied.", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "OK", AnimateHide = false, AnimateShow = false });
                        }
                        else
                        {
                            CancellationToken canceltoken;
                            try
                            {
                                canceltoken = this.cancelsrc.Token;
                            }
                            catch (ObjectDisposedException)
                            {
                                this.CustomDialogResult = false;
                                this.Close();
                                return;
                            }
                            var loginToken = await this.webclient.LoginPSO2Async(id, pw, canceltoken).ConfigureAwait(true);

                            if (loginToken.RequireOTP)
                            {
                                for (int i = 1; i <= OTP_RetryTimes_Safe; i++)
                                {
                                    var otpDialog = new PSO2LoginOtpDialog() { Owner = this };
                                    if (i == 1)
                                    {
                                        otpDialog.DialogMessage = $"Your account has OTP-protection enabled.{Environment.NewLine}Please input OTP to login (Attempt 1/{OTP_RetryTimes_Safe})";
                                    }
                                    else if (i == OTP_RetryTimes_Safe)
                                    {
                                        otpDialog.DialogMessage = $"THIS IS THE LAST ATTEMPT, WRONG OTP FOR {OTP_RetryTimes_Safe + 1} TIMES WILL TEMPORARILY LOCK YOUR ACCOUNT.{Environment.NewLine}Please input OTP to login (Attempt {OTP_RetryTimes_Safe}/{OTP_RetryTimes_Safe})";
                                    }
                                    else
                                    {
                                        otpDialog.DialogMessage = $"Please input OTP to login (Attempt {i}/{OTP_RetryTimes_Safe})";
                                    }
                                    if (otpDialog.ShowCustomDialog(this) == true)
                                    {
                                        // var stuff = NormalizeOTP(_otp);
                                        if (await this.webclient.AuthOTPAsync(loginToken, otpDialog.Otp, canceltoken).ConfigureAwait(true))
                                        {
                                            this._loginToken = loginToken;

                                            otpDialog.ClearPassword();
                                            this.CustomDialogResult = true;
                                            this.Close();
                                            return;
                                        }
                                        else
                                        {
                                            Prompt_Generic.Show(this, $"- Please check your OTP device's date and time.{Environment.NewLine}- Verify if the OTP's system is in sync with the SEGA's system.", "Wrong OTP", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                        }
                                    }
                                    else
                                    {
                                        loginToken.Dispose();
                                        break;
                                    }
                                    otpDialog.ClearPassword();
                                }
                            }
                            else
                            {
                                this._loginToken = loginToken;
                                this.CustomDialogResult = true;
                                this.Close();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Close the dialog and terminate any operations.
                    // this.CustomDialogResult = false;
                    // this.Close();
                }
                catch (PSO2LoginException ex)
                {
                    Prompt_Generic.Show(this, "Failed to login.\r\nError code: " + ex.ErrorCode.ToString(), "Login failure", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                catch (UnexpectedDataFormatException ex)
                {
                    Prompt_Generic.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    Prompt_Generic.ShowError(this, ex);
                }
                finally
                {
                    btn.IsEnabled = true;
                    this.IsInLoading = false;
                }
            }
        }

        // private static string NormalizeOTP(string otpRaw) => otpRaw.Replace(' ', string.Empty[0]);

        private PSO2LoginToken? _loginToken;
        public PSO2LoginToken? LoginToken => this._loginToken;

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

        private void ThisSelf_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.idBox.Text))
            {
                this.idBox.Focus();
            }
            else
            {
                this.pwBox.Focus();
            }
        }
    }
}
