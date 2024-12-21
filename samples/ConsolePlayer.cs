
using System;
using System.Text;
using System.Diagnostics;
class Program
{
     static async Task Main(string[] args)
     {
            var p = new AudioPlayer<NAudio.Wave.WasapiOut>(@"https://www2.cs.uic.edu/~i101/SoundFiles/BabyElephantWalk60.wav");
            p.Play();
            p.IsLoop = false;
            p.OnPositionChanged += new Action<double>((d) => {

                Console.WriteLine(d + "/" + p.Duration);
            });

            while (true)
            {
                var d = Console.ReadKey();

                if (d.Key == ConsoleKey.RightArrow)
                    p.FastForward(0.5f);
                else if (d.Key == ConsoleKey.LeftArrow)
                    p.Rewind(0.5f);

                else if (d.Key == ConsoleKey.Spacebar && p.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                    p.Pause();

                else if (d.Key == ConsoleKey.Spacebar && p.PlaybackState != NAudio.Wave.PlaybackState.Playing)
                    p.Play();

                else if (d.Key == ConsoleKey.UpArrow)
                    Console.WriteLine((int)(p.CurrentPosition / p.Duration * 100) + " %");

            }

            Process.GetCurrentProcess().WaitForExit();
      }
}
