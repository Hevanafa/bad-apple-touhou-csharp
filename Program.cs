using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Timers;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace BadApple
{
    class BadApple
    {
        // Fast Console Buffer base
        // Source:
        // https://stackoverflow.com/questions/2754518/how-can-i-write-fast-colored-output-to-console
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] uint fileAccess,
            [MarshalAs(UnmanagedType.U4)] uint fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] int flags,
            IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteConsoleOutput(
          SafeFileHandle hConsoleOutput,
          CharInfo[] lpBuffer,
          Coord dwBufferSize,
          Coord dwBufferCoord,
          ref SmallRect lpWriteRegion);

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;

            public Coord(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct CharUnion
        {
            [FieldOffset(0)] public char UnicodeChar;
            [FieldOffset(0)] public byte AsciiChar;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharInfo
        {
            [FieldOffset(0)] public CharUnion Char;
            [FieldOffset(2)] public short Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }



        SafeFileHandle h = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
        CharInfo[] buf = new CharInfo[80 * 60];
        SmallRect rect = new SmallRect() { Left = 0, Top = 0, Right = 80, Bottom = 60 };
        Coord C1 = new Coord() { X = 80, Y = 60 },
              C2 = new Coord() { X = 0, Y = 0 };


        WMPLib.WindowsMediaPlayer musicPlayer = new WMPLib.WindowsMediaPlayer();
        // Timer t = new Timer(188); // considering the output latency of 12-14ms
        Timer t = new Timer(30); // Target FPS: 20

        // Source:
        // https://stackoverflow.com/questions/26233781/detect-the-brightness-of-a-pixel-or-the-area-surrounding-it
        double GetBrightness(Color color)
        {
            return (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B);
        }

        // Brightness cells 0-15
        byte[][][] frames;
        string map = " .:;-=+*I%MW&B@#";

        // string[] renderedFrames;

        public BadApple()
        {
            Console.Title = "Bad Apple (Touhou) - By Hevanafa (Mar 2021)";
            Console.SetWindowSize(85, 65);

            Console.Clear();
            Console.SetCursorPosition(0, 0);


            // Scan 1 image & also scan the pixels for the brightness map
            //frames = new byte[1][][];
            //var bmp = new Bitmap("img_20fps_60/0071.png");
            //var dbmp = new DirectBitmap(bmp.Width, bmp.Height);
            //using (var g = Graphics.FromImage(dbmp.Bitmap))
            //    g.DrawImage(bmp, 0, 0);

            //frames[0] = new byte[bmp.Height][];
            //for (var a = 0; a < bmp.Height; a++)
            //    frames[0][a] = new byte[bmp.Width];

            //for (var b = 0; b < dbmp.Height; b++)
            //    for (var a = 0; a < dbmp.Width; a++)
            //        frames[0][b][a] = (byte)(GetBrightness(dbmp.GetPixel(a, b)) / 16);

            // Todo: use fast console buffer

            // Console.WriteLine(string.Join("\n", frames[0].Select(row => string.Join("", row.Select(c => map[c])))));


            // Code to render
            //if (!h.IsInvalid)
            //{
            //    for (var b = 0; b < dbmp.Height; b++)
            //        for (var a = 0; a < dbmp.Width; a++)
            //        {
            //            buf[b * dbmp.Width + a].Attributes = 15;
            //            buf[b * dbmp.Width + a].Char.AsciiChar = (byte)map[frames[0][b][a]];
            //        }

            //    WriteConsoleOutput(h, buf,
            //        new Coord() { X = 80, Y = 60 },
            //        new Coord() { X = 0, Y = 0 },
            //        ref rect);
            //}

            frames = new byte[imgCount][][];
            for (var idx = 0; idx < imgCount; idx++)
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Loading images... " + (idx + 1) + " of " + imgCount);

                var bmp = new Bitmap("img_20fps_60/" + (idx + "").PadLeft(4, '0') + ".png");
                var dbmp = new DirectBitmap(bmp.Width, bmp.Height);
                using (var g = Graphics.FromImage(dbmp.Bitmap))
                    g.DrawImage(bmp, 0, 0);

                // init frame
                frames[idx] = new byte[bmp.Height][];
                for (var a = 0; a < bmp.Height; a++)
                    frames[idx][a] = new byte[bmp.Width];

                // Fill frame with brightness per pixel 0-15
                for (var b = 0; b < dbmp.Height; b++)
                    for (var a = 0; a < dbmp.Width; a++)
                        frames[idx][b][a] = (byte)(GetBrightness(dbmp.GetPixel(a, b)) / 16);

                bmp.Dispose();
                dbmp.Dispose();
            }

            //renderedFrames = new string[1095];
            //for (var idx = 1; idx < 1095; idx++)
            //    renderedFrames[idx] = string.Join("\n", frames[idx].Select(row => string.Join("", row.Select(c => map[c]))));

            // Play the music
            Console.WriteLine("Press Enter to play");
            Console.ReadLine();

            restart();


            // Start the timer
            t.Elapsed += T_Elapsed;

            bool done = false;
            while (!done)
            {
                var cki = Console.ReadKey(true);
                if (end)
                    switch (cki.Key)
                    {
                        case ConsoleKey.R:
                            restart();
                            break;
                        default:
                            done = true; // optional because there's return
                            return;
                    }
            }

            //Console.ReadLine();
        }

        bool end = false;

        void restart()
        {
            t.Stop();
            musicPlayer.controls.stop();

            imgIdx = 0;

            musicPlayer.URL = "music/bad apple.mp3";

            musicPlayer.controls.currentPosition = 0;
            musicPlayer.settings.rate = 1.066; // slightly fast forward to sync with the image
            musicPlayer.controls.play();

            t.Start();
        }

        DateTime beginDraw;

        int imgIdx = 0;
        int imgCount = 6561;

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            beginDraw = DateTime.Now;

            if (imgIdx < imgCount - 1)
                imgIdx++;
            else
            {
                t.Stop();
                Console.Clear();
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Press R to restart, any other key to exit");
                end = true;
                return;
            }

            // Console.SetCursorPosition(0, 0);
            // Console.WriteLine(renderedFrames[imgIdx + 1]);

            Console.SetCursorPosition(0, 61);
            //if (!h.IsInvalid)
            //{
            int cellIdx;
            for (var b = 0; b < 60; b++)
                for (var a = 0; a < 80; a++)
                {
                    cellIdx = b * 80 + a;

                    buf[cellIdx].Attributes = 15;
                    buf[cellIdx].Char.AsciiChar = (byte)map[frames[imgIdx][b][a]];
                }

            WriteConsoleOutput(h, buf, C1, C2, ref rect);
            //}

            Console.WriteLine($"Frame: {imgIdx}/{imgCount}\nLatency: {DateTime.Now.Subtract(beginDraw).TotalMilliseconds}ms");
        }
    }

    // Done: make a bad apple clone
    // Done: scan 1 image
    // Done: load the images
    // Done: scan the images
    // Done: simulate the image cycle with 5 FPS

    // Done: use fast console buffer
    // Done: render with 20 FPS

    // Source:
    // https://stackoverflow.com/questions/24701703/c-sharp-faster-alternatives-to-setpixel-and-getpixel-for-bitmaps-for-windows-f
    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public int[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new int[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
        }

        public void SetPixel(int x, int y, Color colour)
        {
            int index = x + (y * Width);
            int col = colour.ToArgb();

            Bits[index] = col;
        }

        public Color GetPixel(int x, int y)
        {
            int index = x + (y * Width);
            int col = Bits[index];
            Color result = Color.FromArgb(col);

            return result;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }
    }

    class Program
    {
        static void Main() { new BadApple(); Console.ReadLine(); }
    }
}
