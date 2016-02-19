// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   The program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace KeybaordAudio
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Lomont;

    using OpenTK.Audio;
    using OpenTK.Audio.OpenAL;

    using System.Windows.Forms;

    class Program
    {

        static Game game;

        [STAThread]
        static void Main(string[] args)
        {
            /*
            KeyboardWriter writer = new KeyboardWriter();
            
            for(int i = 0; i < 1000; i++)
            {
                writer.Write();
                Thread.Sleep(100);
            }
            */

            Game();
            
        }

        static void Game()
        {
            game = new Game();
            var thread = new Thread(GameThread);
            thread.Start();
            game.HookInput();
        }

        static void Pulse()
        {
            KeyboardWriter writer = new KeyboardWriter();

            var random = new Random();

            var count = 60 + random.Next(21);

            var selections = new List<int>();
            var possibilities = new List<int>();
            var timer = 0;
            var pausetimer = 0;
            var maxtimer = 2000;
            var maxpausetimer = 2000;
            var decayrate = 250;

            var colors = new int[7][];
            colors[0] = new int[] { 255, 0, 0 };
            colors[1] = new int[] { 0, 255, 0 };
            colors[2] = new int[] { 0, 0, 255 };
            colors[3] = new int[] { 255, 255, 0 };
            colors[4] = new int[] { 0, 255, 255 };
            colors[5] = new int[] { 255, 0, 255 };
            colors[6] = new int[] { 255, 255, 255 };

            var color = 0;

            while (true)
            {
                if (timer <= 0)
                {
                    if (pausetimer <= 0)
                    {
                        count = 60 + random.Next(21);
                        selections = new List<int>();
                        possibilities = new List<int>();

                        for (int i = 0; i < 144; i++)
                        {
                            possibilities.Add(i);
                        }
                        for (int i = 0; i < count; i++)
                        {
                            var index = random.Next(possibilities.Count);
                            selections.Add(possibilities[index]);
                            possibilities.RemoveAt(index);
                        }
                        color = random.Next(colors.Length);
                        timer = maxtimer;
                        pausetimer = maxpausetimer;
                    }
                    else
                    {
                        pausetimer -= decayrate;
                    }
                }


                if (timer > 0)
                {
                    for (int i = 0; i < 144; i++) { writer.SetLed(i, 0, 0, 0); }
                    for (int i = 0; i < selections.Count; i++)
                    {
                        var brightness = (int)((float)timer / maxtimer * 7);
                        writer.SetLed(
                            selections[i],
                            colors[color][0] > 0 ? brightness : 0,
                            colors[color][1] > 0 ? brightness : 0,
                            colors[color][2] > 0 ? brightness : 0
                        );
                    }

                    timer -= decayrate;

                    writer.UpdateKeyboard();
                }
            }
        }

        static void GameThread()
        {
            game.Run();
        }
    }
}

