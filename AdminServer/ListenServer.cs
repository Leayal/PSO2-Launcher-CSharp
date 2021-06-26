using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Leayal.SharedInterfaces.Communication;
using Leayal.SharedInterfaces;

namespace Leayal.PSO2Launcher.AdminServer
{
    public class ListenServer
    {
        private readonly UdpClient socket;

        private readonly string check_parentFilename;

        public ListenServer(string parentFilenameToCheck)
        {
            this.check_parentFilename = parentFilenameToCheck;
            this.socket = new UdpClient();
            this.socket.Client.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        }

        public int BindPort
        {
            get
            {
                if (this.socket.Client.LocalEndPoint is IPEndPoint ipport)
                {
                    return ipport.Port;
                }
                return 0;
            }
        }

        public void Listen()
        {
            this.socket.BeginReceive(this.ReceiveCompleted, null);
            // this.socket.BeginAccept
        }

        private void ReceiveCompleted(IAsyncResult ar)
        {
            byte[] packet;
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            try
            {
                packet = this.socket.EndReceive(ar, ref endpoint);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            this.socket.BeginReceive(this.ReceiveCompleted, null);
            try
            {
                if (packet != null)
                {
                    var obj = RestartObj<BootstrapElevation>.DeserializeJson(packet);
                    if (!string.Equals(obj.ParentFilename, this.check_parentFilename, StringComparison.Ordinal))
                    {
                        return;
                    }
                }
            }
            catch
            {

            }
            // endpoint becomes the client address
        }
    }
}
