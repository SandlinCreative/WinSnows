using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WinSnows
{
    public class Global
    {
        public static short numFlakes = 10;   // MAX: 32767
        public static short speed = 2;        // MAX: 32767
        public static short flow = 3;         // MAX: 32767
        public static short wobble = 4;       // MAX: 32767


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowText",
        ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd,
            StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows",
        ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumDesktopWindows(IntPtr hDesktop,
            EnumDelegate lpEnumCallbackFunction, IntPtr lParam);

        // Define the callback delegate's type.
        public delegate bool EnumDelegate(IntPtr hWnd, int lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        private static Rectangle myRect = new Rectangle();
        private static void DoIt(HandleRef hndl)
        {
            RECT rct;

            if (!GetWindowRect(hndl, out rct))
            {
                MessageBox.Show("ERROR");
                return;
            }
            MessageBox.Show(rct.ToString());

            myRect.X = rct.Left;
            myRect.Y = rct.Top;
            myRect.Width = rct.Right - rct.Left;
            myRect.Height = rct.Bottom - rct.Top;
        }
    }


    public partial class MainWindow : Window
    {
        // Save window titles and handles in these lists.
        private static List<IntPtr> WindowHandles;
        private static List<string> WindowTitles;

        // Return a list of the desktop windows' handles and titles.
        public static void GetDesktopWindowHandlesAndTitles(
            out List<IntPtr> handles, out List<string> titles)
        {
            WindowHandles = new List<IntPtr>();
            WindowTitles = new List<string>();

            if (!Global.EnumDesktopWindows(IntPtr.Zero, FilterCallback,
                IntPtr.Zero))
            {
                handles = null;
                titles = null;
            }
            else
            {
                handles = WindowHandles;
                titles = WindowTitles;
            }
        }

        // We use this function to filter windows.
        // This version selects visible windows that have titles.
        private static bool FilterCallback(IntPtr hWnd, int lParam)
        {
            // Get the window's title.
            StringBuilder sb_title = new StringBuilder(1024);
            int length = Global.GetWindowText(hWnd, sb_title, sb_title.Capacity);
            string title = sb_title.ToString();

            // If the window is visible and has a title, save it.
            if (Global.IsWindowVisible(hWnd) &&
                string.IsNullOrEmpty(title) == false)
            {
                WindowHandles.Add(hWnd);
                WindowTitles.Add(title);
            }

            // Return true to indicate that we
            // should continue enumerating windows.
            return true;
        }

        // Display a list of the desktop windows' titles.
        private void ShowDesktopWindows()
        {
            List<IntPtr> handles;
            List<string> titles;
            GetDesktopWindowHandlesAndTitles(
                out handles, out titles);

        }







        private List<Flake> flakes;

        private Random rand = new Random();
        private DispatcherTimer timer = new DispatcherTimer();
        private Stopwatch stopWatch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();

            ShowDesktopWindows();

            double w = System.Windows.SystemParameters.PrimaryScreenWidth;
            double h = System.Windows.SystemParameters.PrimaryScreenHeight;

            //List<WindowScrape.Types.HwndObject> windows = new List<WindowScrape.Types.HwndObject>();
            //foreach (var window in WindowScrape.Types.HwndObject.GetWindows())
            //    if (window.Location.Y > 0 && 
            //        window.Location.Y < h &&
            //        window.Location.X > 0 && 
            //        window.Location.X <= w &&
            //        window.Title != String.Empty &&
            //        window.Title != "Hidden Window")
            //        windows.Add(window);



            flakes = new List<Flake>();

            DrawSnow();

            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += UpdateFlakes;
            timer.Start();
            stopWatch.Start();
        }

        private void UpdateFlakes(object sender, EventArgs e)
        {
            foreach (Flake flake in flakes)
                flake.UpdateFlake(rand);

            if(stopWatch.Elapsed.TotalMilliseconds % Global.flow < 1)
                DrawSnow();

        }

        private void DrawSnow()
        {
            for (int i = 0; i < Global.numFlakes; i++)
            {
                Flake flake = new Flake(rand.Next((int)this.Width - 1));
                flakes.Add(flake);
                theCanvas.Children.Add(flake.flake);
            }
        }
    }

    public class Flake
    {
        public Rectangle flake;
        private short readyForChange = 0;
        private short direction = 0; // 0:straight, -1:left, 1:right
        public Flake(Int32 _startPos)
        {
            flake = new Rectangle();
            flake.Width = 2;
            flake.Height = 2;
            flake.Fill = Brushes.White;
            flake.StrokeThickness = 0;
            Canvas.SetTop(flake, 2);
            Canvas.SetLeft(flake, _startPos);
        }
        public void UpdateFlake(Random _rand)
        {
            Canvas.SetTop(flake, Canvas.GetTop(flake) + Global.speed);

            if (readyForChange++ > Global.wobble)
            {
                direction = (short)_rand.Next(-1,2);
                readyForChange = 0;
            }

            Canvas.SetLeft(flake, Canvas.GetLeft(flake) + direction);
        }
    }
}
