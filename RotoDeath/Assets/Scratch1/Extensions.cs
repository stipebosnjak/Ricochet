using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    public static class Extensions
    {
        public static Vector3 Delta(this Vector3 movement, float speed = 1)
        {
            return movement * Time.deltaTime * speed;
        }

        public static float Delta(this float movement, float speed = 1)
        {
            return movement * Time.deltaTime * speed;
        }

    }
}
