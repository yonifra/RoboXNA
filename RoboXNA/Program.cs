using System;

namespace RoboXNA
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Robo game = new Robo())
            {
                game.Run();
            }
        }
    }
#endif
}

