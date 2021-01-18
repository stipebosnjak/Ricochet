using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DedicatedServer
{
    class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();

        public delegate void PacketHandler(int fromClient, Packet packet);

        public static Dictionary<int, PacketHandler> packetHandlers;

        private static TcpListener _tcpListener;
        private static UdpClient _udpListener;

        public static void Start(int maxPlayers, int port)
        {
            MaxPlayers = maxPlayers;
            Port = port;

            Console.WriteLine("Starting server...");
            InitializeServerData();

            _tcpListener = new TcpListener(IPAddress.Any, Port);
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(TcpConnectCallback, null);

            _udpListener = new UdpClient(Port);
            _udpListener.BeginReceive(UdpReceiveCallback, null);


            Console.WriteLine($"Server started on port {Port}.");
        }

        private static void UdpReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Any, 0);
                var data = _udpListener.EndReceive(ar, ref endpoint);

                _udpListener.BeginReceive(UdpReceiveCallback, null);


                if (data.Length < 4)
                {
                    return;
                }

                using (var packet = new Packet(data))
                {
                    var clientId = packet.ReadInt();

                    if (clientId == 0)
                    {
                        return;
                    }

                    if (clients[clientId].udp.EndPoint == null)
                    {
                        clients[clientId].udp.Connect(endpoint);
                        return;
                    }

                    if (clients[clientId].udp.EndPoint.ToString() == endpoint.ToString())
                    {
                        clients[clientId].udp.HandleData(packet);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void TcpConnectCallback(IAsyncResult result)
        {
            var client = _tcpListener.EndAcceptTcpClient(result);
            _tcpListener.BeginAcceptTcpClient(TcpConnectCallback, null);
            Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}...");

            for (var i = 1; i <= MaxPlayers; i++)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(client);
                    return;
                }
            }

            Console.WriteLine($"{client.Client.RemoteEndPoint} failed to connect: Server full!");
        }

        private static void InitializeServerData()
        {
            for (var i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int) ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived}
            };
            Console.WriteLine("Initialized packets.");
        }

        public static void SendUDPData(IPEndPoint endPoint, Packet packet)
        {
            try
            {
                if (endPoint != null)
                {
                    _udpListener.BeginSend(packet.ToArray(), packet.Length(), endPoint, null, null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error sending data to {endPoint} via UDP: {e}");
                throw;
            }
        }
    }
}