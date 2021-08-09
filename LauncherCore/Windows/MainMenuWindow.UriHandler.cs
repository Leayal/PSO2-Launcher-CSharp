using System;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Windows
{
    partial class MainMenuWindow
    {
        private void ExecuteCommandUrl(Uri url)
        {
            if (url != null && url.IsAbsoluteUri)
            {
                var urlstr = url.AbsoluteUri;
                if (string.Equals(urlstr, "pso2lealauncher://selfupdatechecker/confirm", StringComparison.OrdinalIgnoreCase))
                {
                    this.Close();
                    App.Current.Shutdown();
                    System.Windows.Forms.Application.Restart();
                }
                else if (string.Equals(urlstr, "pso2lealauncher://selfupdatechecker/ignore", StringComparison.OrdinalIgnoreCase))
                {
                    this.SelfUpdateNotification.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
