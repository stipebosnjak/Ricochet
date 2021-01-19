using System.Numerics;

namespace DedicatedServer
{
    class Player
    {
        public int Id;
        public string Username;

        public Vector3 Position { get; set; }
        public Quaternion Rotation;

        private float _moveSpeed = 5f / Constants.TicksPerSec;
                

        public Player(int id, string username, Vector3 spawnPosition)
        {
            Id = id;
            Username = username;
            Position = spawnPosition;
            Rotation = Quaternion.Identity;

        }

        public void Update()
        {
          
        }

        private void Move(Vector2 inputDirection)
        {
            Vector3 forward = Vector3.Transform(new Vector3(0, 0, 1), Rotation);
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));

            Vector3 moveDirection = right * inputDirection.X + forward * inputDirection.Y;
            Position += moveDirection * _moveSpeed;

            ServerSend.PlayerPosition(this);
            ServerSend.PlayerRotation(this);
        }

        public void SetInput(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;

            ServerSend.PlayerPosition(this);
            ServerSend.PlayerRotation(this);
        }
    }
}
