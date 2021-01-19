using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Assets.Scripts
{
    public class Client : MonoBehaviour
    {
        public static Client Instance;
        public static int DataBufferSize = 4096;

        public string ip = "127.0.0.1";
        public int port = 26950;
        public int myId = 0;
        public TcpHelper Tcp;
        public UdpHelper Udp;

        private bool _isConnected = false;
        private delegate void PacketHandler(Packet packet);
        private static Dictionary<int, PacketHandler> _packetHandlers;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Debug.Log("Instance already exists, destroying object!");
                Destroy(this);
            }
        }

        private void Start()
        {
            Tcp = new TcpHelper();
            Udp = new UdpHelper();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        public void ConnectToServer()
        {
            InitializeClientData();

            _isConnected = true;
            Tcp.Connect();
        }

        public class TcpHelper
        {
            public TcpClient Socket;

            private NetworkStream _stream;
            private Packet _receivedData;
            private byte[] _receiveBuffer;

            public void Connect()
            {
                Socket = new TcpClient
                {
                    ReceiveBufferSize = DataBufferSize,
                    SendBufferSize = DataBufferSize
                };

                _receiveBuffer = new byte[DataBufferSize];
                Socket.BeginConnect(Instance.ip, Instance.port, ConnectCallback, Socket);
            }

            private void ConnectCallback(IAsyncResult result)
            {
                Socket.EndConnect(result);

                if (!Socket.Connected)
                {
                    return;
                }

                _stream = Socket.GetStream();

                _receivedData = new Packet();

                _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
            }

            public void SendData(Packet packet)
            {
                try
                {
                    if (Socket != null)
                    {
                        _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
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
                    int byteLength = _stream.EndRead(result);
                    if (byteLength <= 0)
                    {
                        Instance.Disconnect();
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(_receiveBuffer, data, byteLength);

                    _receivedData.Reset(HandleData(data));
                    _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
                }
                catch
                {
                    Disconnect();
                }
            }

            private bool HandleData(byte[] data)
            {
                int packetLength = 0;

                _receivedData.SetBytes(data);

                if (_receivedData.UnreadLength() >= 4)
                {
                    packetLength = _receivedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }

                while (packetLength > 0 && packetLength <= _receivedData.UnreadLength())
                {
                    byte[] packetBytes = _receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet packet = new Packet(packetBytes))
                        {
                            int packetId = packet.ReadInt();
                            _packetHandlers[packetId](packet);
                        }
                    });

                    packetLength = 0;
                    if (_receivedData.UnreadLength() >= 4)
                    {
                        packetLength = _receivedData.ReadInt();
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

            private void Disconnect()
            {
                Instance.Disconnect();

                _stream = null;
                _receivedData = null;
                _receiveBuffer = null;
                Socket = null;
            }
        }

        public class UdpHelper
        {
            public UdpClient Socket;
            public IPEndPoint EndPoint;

            public UdpHelper()
            {
                EndPoint = new IPEndPoint(IPAddress.Parse(Instance.ip), Instance.port);
            }

            public void Connect(int localPort)
            {
                Socket = new UdpClient(localPort);

                Socket.Connect(EndPoint);
                Socket.BeginReceive(ReceiveCallback, null);

                using (Packet packet = new Packet())
                {
                    SendData(packet);
                }
            }

            public void SendData(Packet packet)
            {
                try
                {
                    packet.InsertInt(Instance.myId);
                    if (Socket != null)
                    {
                        Socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"Error sending data to server via UDP: {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    byte[] data = Socket.EndReceive(result, ref EndPoint);
                    Socket.BeginReceive(ReceiveCallback, null);

                    if (data.Length < 4)
                    {
                        Instance.Disconnect();
                        return;
                    }

                    HandleData(data);
                }
                catch
                {
                    Disconnect();
                }
            }

            private void HandleData(byte[] data)
            {
                using (Packet packet = new Packet(data))
                {
                    int packetLength = packet.ReadInt();
                    data = packet.ReadBytes(packetLength);
                }

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(data))
                    {
                        int packetId = packet.ReadInt();
                        _packetHandlers[packetId](packet);
                    }
                });
            }

            private void Disconnect()
            {
                Instance.Disconnect();

                EndPoint = null;
                Socket = null;
            }
        }
    
        private void InitializeClientData()
        {
            _packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ServerPackets.Welcome, ClientHandle.Welcome },
                { (int)ServerPackets.SpawnPlayer, ClientHandle.SpawnPlayer },
                { (int)ServerPackets.PlayerPosition, ClientHandle.PlayerPosition },
                { (int)ServerPackets.PlayerRotation, ClientHandle.PlayerRotation },
            };
            Debug.Log("Initialized packets.");
        }

        private void Disconnect()
        {
            if (_isConnected)
            {
                _isConnected = false;
                Tcp.Socket.Close();
                Udp.Socket.Close();

                Debug.Log("Disconnected from server.");
            }
        }
    }
}
