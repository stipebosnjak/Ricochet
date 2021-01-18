using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DedicatedServer
{
    class Client
    {
        public static int dataBufferSize = 4096;

        public int Id { get; set; }
        public Tcp tcp;
        public Udp udp;
        public Player player;

        public Client(int clientId)
        {
            Id = clientId;
            udp = new Udp(clientId);
            tcp = new Tcp(Id);
        }

        public class Tcp
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            public Tcp(int id)
            {
                this.id = id;
            }

            public void Connect(TcpClient socket)
            {
                this.socket = socket;
                this.socket.ReceiveBufferSize = dataBufferSize;
                this.socket.SendBufferSize = dataBufferSize;

                stream = this.socket.GetStream();

                receivedData = new Packet();
                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

                ServerSend.Welcome(id, "Welcome to the server!");
            }

            public void SendData(Packet packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    var byteLength = stream.EndRead(result);
                    if (byteLength <= 0)
                    {
                        // TODO: disconnect
                        return;
                    }

                    var data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    receivedData.Reset(HandleData(data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {ex}");
                    // TODO: disconnect
                }
            }

            private bool HandleData(byte[] data)
            {
                var packetLength = 0;

                receivedData.SetBytes(data);

                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }

                while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
                {
                    var packetBytes = receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (var packet = new Packet(packetBytes))
                        {
                            var packetId = packet.ReadInt();
                            Server.packetHandlers[packetId](id, packet);
                        }
                    });

                    packetLength = 0;
                    if (receivedData.UnreadLength() >= 4)
                    {
                        packetLength = receivedData.ReadInt();
                        if (packetLength <= 0)
                        {
                            return true;
                        }
                    }
                }

                if (packetLength <= 1)
                {
                    return true;
                }

                return false;
            }
        }

        public class Udp
        {
            public IPEndPoint EndPoint { get; set; }

            private int Id { get; set; }

            public Udp(int id)
            {
                Id = id;
            }

            public void Connect(IPEndPoint endPoint)
            {
                EndPoint = endPoint;
            }

            public void SendData(Packet packet)
            {
                Server.SendUDPData(EndPoint, packet);
            }

            // private void ReceiveCallback(IAsyncResult ar)
            // {
            //     try
            //     {
            //         var data = socket.EndReceive(ar, ref endPoint);
            //
            //         socket.BeginReceive(ReceiveCallback, null);
            //
            //         if (data.Length < 4)
            //         {
            //             return;
            //         }
            //
            //         HandleData(data);
            //     }
            //     catch (Exception e)
            //     {
            //         Console.WriteLine(e);
            //         throw;
            //     }
            // }

            public void HandleData(Packet packetData)
            {
                var packetLength = packetData.ReadInt();
                var data = packetData.ReadBytes(packetLength);


                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (var packet = new Packet(data))
                    {
                        var packetId = packet.ReadInt();
                        Server.packetHandlers[packetId](Id, packet);
                    }
                });
            }
        }

        public void SendIntoGame(string playerName)
        {
            player = new Player(Id, playerName, new Vector3());

            foreach (Client client in Server.clients.Values)
            {
                if (client.player != null)
                {
                    if (client.Id != Id)
                    {
                        ServerSend.SpawnPlayer(Id, client.player);
                    }
                }
            }

            foreach (Client client in Server.clients.Values)
            {
                if (client.player != null)
                {
                    ServerSend.SpawnPlayer(client.Id, player);
                }
            }
        }
    }
}