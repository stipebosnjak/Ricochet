using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        private void FixedUpdate()
        {
            SendInputToServer();
        }

        private void SendInputToServer()
        {
            ClientSend.PlayerMovement(new PlayerTransform()
            {
                Position = transform.position,
                Rotation = transform.rotation
            });
        }
    }
}