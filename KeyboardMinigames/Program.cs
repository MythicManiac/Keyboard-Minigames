namespace KeyboardMinigames
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    class Program
    {

        static Game game;

        [STAThread]
        static void Main(string[] args)
        {
            Game();
        }

        static void Game()
        {
            game = new Game();
            var thread = new Thread(GameThread);
            thread.Start();
            game.HookInput();
        }

        static void GameThread()
        {
            game.Run();
        }
    }
}

