using Leayal.SharedInterfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public static class AdminProcess
    {
        internal static readonly byte[] ReportSign = Encoding.UTF8.GetBytes("lea-admin-host-process-report");

        private static int state;
        private static Task<int> t_adminLaunch;
        private static TcpClient serverInstance;
        private static NetworkStream connectionStreamToKeepServerAlive;

        static AdminProcess()
        {
            Interlocked.Exchange(ref state, 0);
        }

        public static void TerminateAdminProcess()
        {
            connectionStreamToKeepServerAlive?.Close();
            serverInstance?.Dispose();
        }

        public static Task<int> StartServerProcess()
        {
            if (Interlocked.CompareExchange(ref state, 1, 0) == 0)
            {
                t_adminLaunch = CreateHostProcess();
            }
            return t_adminLaunch;
        }

        public static async Task<AdminClient> CreateElevator()
        {
            var adminport = await StartServerProcess();
            var client = new AdminClient();
            client.Connect(adminport);
            return client;
        }

        private static async Task<int> CreateHostProcess()
        {
            using (var proc = new Process())
            {
                TcpListener reportHandler = null;
                try
                {
                    reportHandler = new TcpListener(IPAddress.Loopback, 0);

                    reportHandler.Start();
                    var ip = (IPEndPoint)reportHandler.LocalEndpoint;
                    proc.StartInfo.FileName = RuntimeValues.EntryExecutableFilename;
                    proc.StartInfo.UseShellExecute = true;
                    proc.StartInfo.Arguments = "--admin-host-process " + ip.Port.ToString();
                    proc.StartInfo.Verb = "runas";

                    proc.Start();

                    serverInstance = await reportHandler.AcceptTcpClientAsync();
                    connectionStreamToKeepServerAlive = serverInstance.GetStream();

                    var sizeOfInt = sizeof(int);
                    var buffer = new byte[ReportSign.Length + sizeOfInt];
                    if (connectionStreamToKeepServerAlive.Read(buffer, 0, buffer.Length) == buffer.Length &&
                        buffer.AsSpan(0, ReportSign.Length).SequenceEqual(ReportSign))
                    {
                        return BitConverter.ToInt32(buffer.AsSpan(ReportSign.Length));
                    }
                    else
                    {
                        connectionStreamToKeepServerAlive.Close();
                    }

                }
                finally
                {
                    reportHandler?.Stop();
                }
            }
            return 0;
        }

        private static ListenServer adminCommandHandlerServer;

        public static bool Host(string[] args)
        {
            if (args != null && args.Length > 1)
            {
                if (string.Equals(args[0], "--admin-host-process", StringComparison.Ordinal) &&
                    int.TryParse(args[1], out var reportPort))
                {
                    if (adminCommandHandlerServer == null)
                    {
                        adminCommandHandlerServer = new ListenServer();
                    }
                    adminCommandHandlerServer.Listen();
                    adminCommandHandlerServer.ReportIn(reportPort);

                    return true;
                }
            }
            return false;
        }

        public static bool Host2(string[] args)
        {
            if (args != null && args.Length > 1)
            {
                if (string.Equals(args[0], "--admin-host-process", StringComparison.Ordinal) &&
                    int.TryParse(args[1], out var reportWindowHandle))
                {
                    var form = new DummyForm();
                    form.IPCBufferReceived += Form_IPCBufferReceived;
                    if (form.ListenForMessage())
                    {
                        form.SendDataTo(new IntPtr(reportWindowHandle), 0, null);
                        Application.Run();
                    }
                    return true;
                }
            }
            return false;
        }

        private static void Form_IPCBufferReceived(IntPtr senderWindowHandle, DummyForm.BorrowedBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}
