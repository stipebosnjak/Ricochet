using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class Client : MonoBehaviour
    {
        public static Client instance;
        public static int dataBufferSize = 4096;

        public string ip = "127.0.0.1";
        public int port = 26950;
        public int Id { get; set; }
        public Tcp tcp;
        public Udp udp;

        private delegate void PacketHandler(Packet packet);

        private static Dictionary<int, PacketHandler> _packetHandlers;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Debug.Log("Instance already exists, destroying object!");
                Destroy(this);
            }
        }

        private void Start()
        {
            tcp = new Tcp();
            udp = new Udp();
        }

        public void ConnectToServer()
        {
            InitializeClientData();

            tcp.Connect();
        }

        public class Tcp
        {
            public TcpClient socket;

            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            public void Connect()
            {
                socket = new TcpClient
                {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };

                receiveBuffer = new byte[dataBufferSize];
                socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
            }

            private void ConnectCallback(IAsyncResult result)
            {
                socket.EndConnect(result);

                if (!socket.Connected)
                {
                    return;
                }

                stream = socket.GetStream();

                receivedData = new Packet();

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
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
                    Debug.Log($"Error sending data to server via TCP: {ex}");
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
                catch
                {
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
                        using var packet = new Packet(packetBytes);
                        var packetId = packet.ReadInt();
                        _packetHandlers[packetId](packet);
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
            public UdpClient socket;
            public IPEndPoint endPoint;

            public Udp()
            {
                endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
            }

            public void Connect(int localPort)
            {
                socket = new UdpClient(localPort);

                socket.Connect(endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                using var packet = new Packet();
                SendData(packet);
            }

            public void SendData(Packet packet)
            {
                try
                {
                    packet.InsertInt(instance.Id);
                    if (socket != null)
                    {
                        socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error sending data to server via UDP {e}");
                    throw;
                }
            }

            private void ReceiveCallback(IAsyncResult ar)
            {
                try
                {
                    var data = socket.EndReceive(ar, ref endPoint);

                    socket.BeginReceive(ReceiveCallback, null);

                    if (data.Length < 4)
                    {
                        return;
                    }

                    HandleData(data);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            private void HandleData(byte[] data)
            {
                using var packet = new Packet(data);
                var packetLength = packet.ReadInt();

                data = packet.ReadBytes(packetLength);


                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using var packet = new Packet(data);
                    var packetId = packet.ReadInt();
                    _packetHandlers[packetId](packet);
                });
            }
        }

        private void InitializeClientData()
        {
            _packetHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int) ServerPackets.welcome, ClientHandle.Welcome}

            };
            Debug.Log("Initialized packets.");
        }
    }
}