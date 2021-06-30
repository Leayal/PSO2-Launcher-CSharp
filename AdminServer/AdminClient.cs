using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.AdminProcess
{
    public class AdminClient
    {
        private readonly TcpClient client;
        private NetworkStream stream;

        public AdminClient()
        {
            this.client = new TcpClient();
        }

        public void Connect(int port)
        {
            this.client.Connect(IPAddress.Loopback, port);
            this.stream = this.client.GetStream();
            // Forgot to do handshake...
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(ListenServer.HandshakeRequestPacket.Length);
            try
            {
                if (this.stream.Read(buffer, 0, ListenServer.HandshakeRequestPacket.Length) == ListenServer.HandshakeRequestPacket.Length &&
                    buffer.AsSpan(0, ListenServer.HandshakeRequestPacket.Length).SequenceEqual(ListenServer.HandshakeRequestPacket))
                {
                    this.stream.Write(ListenServer.HandshakeRequestResponse);
                }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
            
        }

        public void Disconnect()
        {
            this.stream.Close();
            this.client.Close();
        }

        public async Task<ResponseElevateProcess> ElevateProcess(CommandElevateProcess command)
        {
            return await this.SendCommand<ResponseElevateProcess>(command);
        }

        public async Task<T> SendCommand<T>(CommandPacket command) where T : Response, new()
        {
            if (!this.client.Connected)
            {
                throw new InvalidOperationException();
            }

            var packet = command.Encode();
            var sizeOfInt = sizeof(int);
            var messageSize = packet.Length + sizeOfInt;
            var messageWithHeader = System.Buffers.ArrayPool<byte>.Shared.Rent(messageSize);
            try
            {
                BitConverter.GetBytes(sizeOfInt).CopyTo(messageWithHeader, 0);
                packet.CopyTo(new Memory<byte>(messageWithHeader, sizeOfInt, packet.Length));
                await this.stream.WriteAsync(messageWithHeader, 0, messageSize);
                var responseHeader = this.stream.Read(messageWithHeader, 0, sizeOfInt);
                if (responseHeader == sizeOfInt)
                {
                    var responsePacketLength = BitConverter.ToInt32(messageWithHeader, 0);
                    if (messageWithHeader.Length < responsePacketLength)
                    {
                        var responseBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(responsePacketLength);
                        try
                        {
                            var readbytes = await this.stream.ReadAsync(responseBuffer, 0, responsePacketLength);
                            if (readbytes == responsePacketLength)
                            {
                                T reponse = new T();
                                reponse.Decode(responseBuffer);

                                return reponse;
                            }
                        }
                        finally
                        {
                            System.Buffers.ArrayPool<byte>.Shared.Return(responseBuffer);
                        }
                    }
                    else
                    {
                        var readbytes = await this.stream.ReadAsync(messageWithHeader, 0, responsePacketLength);
                        if (readbytes == responsePacketLength)
                        {
                            T reponse = new T();
                            reponse.Decode(messageWithHeader);

                            return reponse;
                        }
                    }
                }
                this.Disconnect();
                return null;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(messageWithHeader);
            }
        }
    }
}
