using UnityEngine;

namespace Assets.Scripts
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
