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
            bool[] _inputs = new bool[]
            {
                Input.GetKey(KeyCode.W),
                Input.GetKey(KeyCode.S),
                Input.GetKey(KeyCode.A),
                Input.GetKey(KeyCode.D),
            };

           // ClientSend.PlayerMovement(_inputs);
        }
    }
}
