using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DedicatedServer
{
    public class GameLogic
    {
        public static void Update()
        {
            ThreadManager.UpdateMain();
        }
    }
}