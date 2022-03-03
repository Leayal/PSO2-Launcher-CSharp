using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public static partial class AdminProcess
    {
        private static BinaryWriter? _clientStreamWriter;

        internal static async Task InitializeElevatedClient(string handle)
        {
            _clientStream = new AnonymousPipeClientStream(PipeDirection.Out, handle);
            _clientStreamWriter = new BinaryWriter(_clientStream);
            _clientStreamWriter.Write();
        }
    }
}