// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   Defines the ConsoleWriter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace KeybaordAudio
{
    using System;
    using System.Linq;
    using System.Text;

    public class ConsoleWriter : IWriter
    {
        public void Write(int iter, byte[] fftData)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();

            for (int colIdx = 0; colIdx < fftData.Length / 2; colIdx += 2)
            {
                var sb = new StringBuilder();
                // Prevent attempting to write spectrogram outside of consoel bounds
                var lineLength = Math.Min(this.Average(fftData[colIdx], fftData[colIdx + 1]), Console.BufferWidth);

                for (int rowIdx = 0; rowIdx < lineLength; rowIdx += 2)
                {
                    sb.Append('=');
                }

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.WriteLine(sb.ToString());
            }
        }

        private double Average(params double[] list)
        {
            return list.Average();
        }
    }
}
