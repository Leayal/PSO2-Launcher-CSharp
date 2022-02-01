using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Leayal.PSO2Launcher.Core.Classes.PSO2;
using System.Threading;

namespace Leayal.PSO2Launcher.Core.Classes
{
    class PSO2DataMover
    {
        private readonly BlockingCollection<string> Items;
        private readonly PSO2HttpClient client;

        public PSO2DataMover(PSO2HttpClient http)
        {
            this.client = http;
        }

        private async Task<PatchListMemory> GetFileList(GameClientSelection selection, CancellationToken token)
        {
            var t_list_launcher = this.client.GetLauncherListAsync(token);
            Task<PatchListMemory> t_nextList;
            switch (selection)
            {
                case GameClientSelection.NGS_AND_CLASSIC:
                    t_nextList = this.client.GetPatchListAllAsync(token);
                    break;
                case GameClientSelection.NGS_Only:
                case GameClientSelection.NGS_Prologue_Only:
                    t_nextList = this.client.GetPatchListNGSFullAsync(token);
                    break;
                case GameClientSelection.Classic_Only:
                    t_nextList = this.client.GetPatchListClassicAsync(token);
                    break;
                default:
                    throw new NotSupportedException();
            }
            return PatchListBase.Create(await t_list_launcher, await t_nextList);
        }

        public void MoveFilesTo(string output, GameClientSelection selection, CancellationToken token)
        {

        }
    }
}
