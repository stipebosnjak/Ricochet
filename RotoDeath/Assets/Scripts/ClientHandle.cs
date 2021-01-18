using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class ClientHandle : MonoBehaviour
    {
        public static void Welcome(Packet packet)
        {
            var msg = packet.ReadString();
            var id = packet.ReadInt();


            Debug.Log($"Message from server: {msg}");
            Client.instance.Id = id;
            ClientSend.WelcomeReceived();

            Client.instance.udp.Connect(((IPEndPoint) Client.instance.tcp.socket.Client.LocalEndPoint).Port);
        }
    }
}