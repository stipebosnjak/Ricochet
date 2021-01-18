using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class ClientSend : MonoBehaviour
    {
        private static void SendTcpData(Packet packet)
        {
            packet.WriteLength();
            Client.instance.tcp.SendData(packet);
        }

        private static void SendUdpData(Packet packet)
        {
            packet.WriteLength();

            Client.instance.udp.SendData(packet);
        }

        #region Packets

        public static void WelcomeReceived()
        {
            using var packet = new Packet((int) ClientPackets.welcomeReceived);
            packet.Write(Client.instance.Id);
            packet.Write(UiManager.instance.userNameField.text);

            SendTcpData(packet);
        }

        #endregion
    }
}