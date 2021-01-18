using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DedicatedServer
{
    public class ServerSend
    {
        public static void Welcome(int toClient, string msg)
        {
            using (var packet = new Packet((int) ServerPackets.welcome))
            {
                packet.Write(msg);
                packet.Write(toClient);

                SendTcpData(toClient, packet);
            }
        }

        private static void SendTcpData(int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.clients[toClient].tcp.SendData(packet);
        }

        private static void SendTcpDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (var i = 1; i < Server.MaxPlayers; i++)
            {
                if (Server.clients[i] != null)
                    Server.clients[i].tcp.SendData(packet);
            }
        }

        private static void SendTcpDataToAll(int exceptClient, Packet packet)
        {
            packet.WriteLength();
            for (var i = 1; i < Server.MaxPlayers; i++)
            {
                if (i != exceptClient)
                    Server.clients[i].tcp.SendData(packet);
            }
        }

        public static void SendUDPData(int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.clients[toClient].udp.SendData(packet);
        }


        private static void SendUdpDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (var i = 1; i < Server.MaxPlayers; i++)
            {
                if (Server.clients[i] != null)
                    Server.clients[i].udp.SendData(packet);
            }
        }

        private static void SendUdpDataToAll(int exceptClient, Packet packet)
        {
            packet.WriteLength();
            for (var i = 1; i < Server.MaxPlayers; i++)
            {
                if (i != exceptClient)
                    Server.clients[i].udp.SendData(packet);
            }
        }


        public static void SpawnPlayer(int id, Player player)
        {
            Packet.NewPacket(packet =>
            {
                packet.Write(player.Id);
                packet.Write(player.Username);
                packet.Write(player.Position);
                packet.Write(player.Rotation);
                
            }, ServerPackets.spawnPlayer);


           
        }
    }
}