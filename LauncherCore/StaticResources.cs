using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core
{
    static class StaticResources
    {
        public static readonly Uri Url_ConfirmSelfUpdate = new Uri("pso2lealauncher://selfupdatechecker/confirm");
        public static readonly Uri Url_IgnoreSelfUpdate = new Uri("pso2lealauncher://selfupdatechecker/ignore");

        public static readonly Uri Url_ShowAuthor = new Uri("pso2lealauncher://myself/show-author");
        public static readonly Uri Url_ShowSourceCodeGithub = new Uri("pso2lealauncher://myself/show-source-code");
        public static readonly Uri Url_ShowLatestGithubRelease = new Uri("pso2lealauncher://myself/show-latest-github-release");
        public static readonly Uri Url_ShowIssuesGithub = new Uri("pso2lealauncher://myself/show-github-issues");

        public static readonly Uri Url_OpenWebView2InstallerDownloadPage = new Uri("pso2lealauncher://myself/show-webview2-downloadpage");
        public static readonly Uri Url_DownloadWebView2BootstrapInstaller = new Uri("pso2lealauncher://myself/download-webview2-bootstrapinstaller");

        public static readonly Uri Url_ShowLogDialogFromGuid = new Uri("pso2lealauncher://showdialog.fromguid/");

        public static readonly Uri Url_ShowPathInExplorer_SpecialFolder_JP_PSO2Config = new Uri("pso2lealauncher://showpathinexplorer/specialfolder/jp/pso2config");

        public static readonly Uri Url_Toolbox_AlphaReactorCounter = new Uri("pso2lealauncher://toolbox/alphareactorcounter");
    }
}
