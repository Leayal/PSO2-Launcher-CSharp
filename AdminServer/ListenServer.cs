using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Leayal.SharedInterfaces.Communication;
using Leayal.SharedInterfaces;
using System.Buffers;
using System.IO;

namespace Leayal.PSO2Launcher.AdminProcess
{
    /// <summary>
    /// Handles multiple clients for concurrent.
    /// </summary>
    public class ListenServer : IDisposable
    {
        internal static readonly byte[] HandshakeRequestPacket = System.Text.Encoding.UTF8.GetBytes("identify-yourself");
        internal static readonly byte[] HandshakeRequestResponse = System.Text.Encoding.UTF8.GetBytes("no-one-but-lea-client");

        private readonly TcpListener listener;
        private int state;

        private int indexing;
        private readonly ConcurrentDictionary<int, ClientItem> clients;

        public ListenServer()
        {
            this.indexing = 0;
            this.state = 0;
            this.clients = new ConcurrentDictionary<int, ClientItem>();
            this.listener = new TcpListener(IPAddress.Loopback, 0);
        }

        public int BindPort
        {
            get
            {
                if (this.listener.Server.LocalEndPoint is IPEndPoint ipport)
                {
                    return ipport.Port;
                }
                return 0;
            }
        }

        public void ReportIn(int port)
        {
            using (var reportClient = new TcpClient(IPAddress.Loopback.ToString(), port))
            using (var stream = reportClient.GetStream())
            {
                var sizeOfInt = sizeof(int);
                var buffer = new byte[AdminProcess.ReportSign.Length + sizeOfInt];
                AdminProcess.ReportSign.CopyTo(buffer, 0);
                BitConverter.GetBytes(this.BindPort).CopyTo(buffer, AdminProcess.ReportSign.Length);

                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
                try
                {
                    stream.Read(buffer, 0, 1);
                }
                catch
                {
                    
                }
            }
        }

        public void Listen()
        {
            var old_state = Interlocked.CompareExchange(ref this.state, 1, 0);
            switch (old_state)
            {
                case 0:
                    this.listener.Start();
                    this.listener.BeginAcceptTcpClient(this.ReceiveCompleted, null);
                    break;
            }
        }

        public void Stop()
        {
            var old_state = Interlocked.CompareExchange(ref this.state, 0, 1);
            switch (old_state)
            {
                case 1:
                    this.listener.Stop();
                    this.DisconnectAllClients();
                    break;
            }
        }

        public void DisconnectAllClients()
        {
            if (Interlocked.Exchange(ref this.indexing, 0) != 0)
            {
                var copied = this.clients.ToArray();
                this.clients.Clear();
                Interlocked.Exchange(ref this.indexing, 0);
                foreach (var pair in copied)
                {
                    try
                    {
                        pair.Value.Stream.Close();
                        pair.Value.Client.Close();
                        pair.Value.Client.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void Dispose()
        {
            this.Stop();
        }

        private void ReceiveCompleted(IAsyncResult ar)
        {
            try
            {
                var _client = this.listener.EndAcceptTcpClient(ar);
                this.AcceptClientAndPerformHandshake(_client);
            }
            catch (ObjectDisposedException)
            {
                this.Stop();
            }
        }

        private void AcceptClientAndPerformHandshake(TcpClient _client)
        {
            try
            {
                if (_client.Client.RemoteEndPoint is IPEndPoint ip)
                {
                    if (true || ip.Address == IPAddress.Loopback)
                    {
                        var added = Interlocked.Increment(ref this.indexing);
                        var stream = _client.GetStream();
                        if (!this.clients.TryAdd(added, new ClientItem(_client, stream)))
                        {
                            stream.Close();
                            _client.Close();
                            _client.Dispose();
                        }
                        else
                        {
                            stream.BeginWrite(HandshakeRequestPacket, 0, HandshakeRequestPacket.Length, this.HandshakeClient, added);
                        }
                        this.listener.BeginAcceptTcpClient(this.ReceiveCompleted, null);
                    }
                    else
                    {
                        _client.Close();
                        _client.Dispose();
                    }
                }
                else
                {
                    _client.Close();
                    _client.Dispose();
                }
            }
            catch (ObjectDisposedException)
            {
                this.Stop();
            }
        }

        private void HandshakeClient(IAsyncResult ar)
        {
            var client_index = (int)ar.AsyncState;
            var client = this.clients[client_index];
            try
            {
                var stream = client.Stream;
                stream.EndWrite(ar);
                var buffer = new byte[HandshakeRequestResponse.Length];
                if (stream.Read(buffer, 0, buffer.Length) == HandshakeRequestResponse.Length && 
                    buffer.AsSpan().SequenceEqual(HandshakeRequestResponse))
                {
                    // Handshake complete
                    // Listen for incoming request.
                    var state = new ClientMessageReceivingState(client_index, client);
                    var sizeHeader = state.HeaderBuffer;
                    stream.BeginRead(sizeHeader, 0, sizeHeader.Length, this.Client_OnPacketReceived, state);
                }
                else
                {
                    // Yeet fake
                    stream.Dispose();
                    client.Client.Close();
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static void WeirdDebugWay(string something)
        {
            using (var sw = new StreamWriter(@"E:\All Content\VB_Project\visual studio 2019\PSO2-Launcher-CSharp\Test\error.txt", true))
            {
                sw.WriteLine(something);
            }
        }

        private async void Client_OnPacketReceived(IAsyncResult ar)
        {
            var state = (ClientMessageReceivingState)ar.AsyncState;
            var client = state.ClientInfo.Client;
            var stream = state.ClientInfo.Stream;
            try
            {
                var headerSize = stream.EndRead(ar);
                var sizeOfInt = sizeof(int);
                WeirdDebugWay($"||{headerSize}:{sizeOfInt}");
                if (headerSize == sizeOfInt)
                {
                    var packetSize = BitConverter.ToInt32(state.HeaderBuffer);
                    WeirdDebugWay("-2:" + packetSize.ToString());
                    byte[] packetBuffer = null;
                    try
                    {
                        packetBuffer = ArrayPool<byte>.Shared.Rent(packetSize);
                        WeirdDebugWay("-1");
                        if (stream.Read(packetBuffer, 0, packetSize) == packetSize)
                        {
                            WeirdDebugWay("0");
                            if (CommandElevateProcess.TryDecodeData(packetBuffer.AsMemory(0, packetSize), out var command_elevate))
                            {
                                WeirdDebugWay("1");
                                var rep = await command_elevate.Execute();
                                WeirdDebugWay("2");
                                var responsePacket = rep.Encode();
                                WeirdDebugWay("3");
                                var messageWithHeader = new byte[responsePacket.Length + sizeOfInt];
                                WeirdDebugWay("4");
                                BitConverter.GetBytes(responsePacket.Length).CopyTo(messageWithHeader, 0);
                                responsePacket.CopyTo(new Memory<byte>(messageWithHeader, sizeOfInt, responsePacket.Length));
                                stream.Write(messageWithHeader, 0, messageWithHeader.Length);
                                WeirdDebugWay("5");
                            }
                            else
                            {
                                // Response unknown command.
                            }
                            var sizeHeader = state.HeaderBuffer;
                            stream.BeginRead(sizeHeader, 0, sizeHeader.Length, this.Client_OnPacketReceived, state);
                        }
                        else
                        {
                            // Response corrupted packet. Can resend or cancel it.
                        }
                    }
                    finally
                    {
                        if (packetBuffer != null)
                        {
                            ArrayPool<byte>.Shared.Return(packetBuffer);
                        }
                    }
                }
                else
                {
                    stream.Close();
                    client.Close();
                    client.Dispose();
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        class ClientMessageReceivingState
        {
            public readonly int ClientIndex;
            public readonly ClientItem ClientInfo;
            public readonly byte[] HeaderBuffer;

            public ClientMessageReceivingState(int client_index, ClientItem clientinfo)
            {
                this.ClientIndex = client_index;
                this.ClientInfo = clientinfo;
                this.HeaderBuffer = new byte[sizeof(int)];
            }
        }

        class ClientItem
        {
            public readonly TcpClient Client;
            public readonly NetworkStream Stream;

            public ClientItem(TcpClient client, NetworkStream stream)
            {
                this.Client = client;
                this.Stream = stream;
            }
        }
    }
}
