using System;
using System.Collections.Generic;
using System.Threading;
using CUE.NET;

namespace KeyboardMinigames
{
    class Program
    {
        private static Game _game;

        private static Dictionary<string, Type> _games = new Dictionary<string, Type>(){
            {"snake", typeof(SnakeGame)}
        };

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 1) { return; }
            var type = args[0];
            CueSDK.Initialize();
            Game(type);
        }

        static void Game(string type)
        {
            if (!_games.ContainsKey(type)) { return; }
            var gameType = _games[type];
            _game = (Game)Activator.CreateInstance(gameType);
            var thread = new Thread(GameThread);
            thread.Start();
            _game.InitializeInput();
        }

        static void GameThread()
        {
            _game.Run();
        }
    }
}

