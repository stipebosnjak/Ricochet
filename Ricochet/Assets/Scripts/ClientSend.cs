using UnityEngine;

namespace Assets.Scripts
{
    public class ClientSend : MonoBehaviour
    {
        private static void SendTcpData(Packet packet)
        {
            packet.WriteLength();
            Client.Instance.Tcp.SendData(packet);
        }

        private static void SendUdpData(Packet packet)
        {
            packet.WriteLength();
            Client.Instance.Udp.SendData(packet);
        }

        #region Packets

        public static void WelcomeReceived()
        {
            using (Packet packet = new Packet((int) ClientPackets.WelcomeReceived))
            {
                packet.Write(Client.Instance.myId);
                packet.Write(UiManager.Instance.usernameField.text);

                SendTcpData(packet);
            }
        }

        public static void PlayerMovement(PlayerTransform input)
        {
            using (Packet packet = new Packet((int) ClientPackets.PlayerMovement))
            {
                packet.Write(input.Position);
                packet.Write(input.Rotation);
                packet.Write(GameManager.Players[Client.Instance.myId].transform.rotation);

                SendUdpData(packet);
            }
        }

        #endregion
    }

    public class PlayerTransform

    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }
}