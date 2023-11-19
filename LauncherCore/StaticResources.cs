using System;

namespace Leayal.PSO2Launcher.Core
{
    static class StaticResources
    {
        public static readonly Uri Url_ConfirmSelfUpdate = new Uri("pso2lealauncher://selfupdatechecker/confirm"),
            Url_IgnoreSelfUpdate = new Uri("pso2lealauncher://selfupdatechecker/ignore"),

            Url_ShowAuthor = new Uri("pso2lealauncher://myself/show-author"),
            Url_ShowSourceCodeGithub = new Uri("pso2lealauncher://myself/show-source-code"),
            Url_ShowLatestGithubRelease = new Uri("pso2lealauncher://myself/show-latest-github-release"),
            Url_ShowIssuesGithub = new Uri("pso2lealauncher://myself/show-github-issues"),
            
            Url_OpenWebView2InstallerDownloadPage = new Uri("pso2lealauncher://myself/show-webview2-downloadpage"),
            Url_DownloadWebView2BootstrapInstaller = new Uri("pso2lealauncher://myself/download-webview2-bootstrapinstaller"),
            
            Url_ShowLogDialogFromGuid = new Uri("pso2lealauncher://showdialog.fromguid/"),
            
            Url_ShowPathInExplorer_SpecialFolder_JP_PSO2Config = new Uri("pso2lealauncher://showpathinexplorer/specialfolder/jp/pso2config"),
            
            Url_Toolbox_VendorItemPickupCounter = new Uri("pso2lealauncher://toolbox/vendoritempickupcounter"),
            Url_Toolbox_PSO2DataOrganizer = new Uri("pso2lealauncher://toolbox/pso2dataorganizer"),
            
            SEGALauncherNewsUrl = new Uri("https://launcher.pso2.jp/ngs/01/"),
            WellbiaFAQWebsite = new Uri("https://wellbia.com/?module=Board&action=SiteBoard&sMode=SELECT_FORM&iBrdNo=3");

        /*
        public static readonly bool IsCurrentProcessAdmin = Leayal.Shared.UacHelper.IsCurrentProcessElevated;

        static StaticResources()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                IsCurrentProcessAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                principal = null;
            }
        }
        */
    }
}
