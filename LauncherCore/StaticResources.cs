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

        public static readonly Uri Url_ShowPathInExplorer_SpecialFolder_JP_PSO2Config = new Uri("pso2lealauncher://showpathinexplorer/specialfolder/jp/pso2config");
    }
}
