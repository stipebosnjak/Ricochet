using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DedicatedServer
{
    public class Player
    {
        public int Id { get; set; }
        public string Username { get; set; }

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public Player(int id, string username,Vector3 position  )
        {
            Position = position;
            Rotation = Quaternion.Identity;
            Id = id;
            Username = username;
        }
    }
}
