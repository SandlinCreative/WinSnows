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
        public static int flakeLimit = 6000;

        public static Dictionary<double, StopPoint> stopPoints = new Dictionary<double, StopPoint>();

        #region Window Stuff

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

            myRect.Width = rct.Right - rct.Left;
            myRect.Height = rct.Bottom - rct.Top;
        }
        #endregion
    }


    public partial class MainWindow : Window
    {

        #region Window Stuff
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

        #endregion

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

            SecondaryWindow win2 = new SecondaryWindow();
            //win2.Show();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            flakes = new List<Flake>();

            DrawSnow();

            timer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / 60); //update at 60 fps
            timer.Tick += UpdateFlakes;
            timer.Start();
            stopWatch.Start();
        }

        private void UpdateFlakes(object sender, EventArgs e)
        {
            foreach (Flake flake in flakes)
                flake.UpdateFlake(rand);

            if (flakes.Count > Global.flakeLimit)
            {
                theCanvas.Children.RemoveRange(0,Global.flakeLimit / 2);
                flakes.RemoveRange(0, Global.flakeLimit / 2);
                Console.WriteLine("FLAKES REMOVED");
            }

            if (stopWatch.Elapsed.TotalMilliseconds % Global.flow < 1)
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

    public class StopPoint
    {
        public double X;
        public double Y;
        public double ID { get { return X * Y + X; } }
        public int Count;
        public StopPoint(double _x, double _y)
        {
            X = _x;
            Y = _y;
        }
    }

    public class Flake
    {
        public Rectangle flake;
        private short readyForChange = 0;
        private float direction = 0; // 0:straight, -1:left, 1:right
        private bool atRest = false;
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
            if (atRest) return;

            int x = (int)Canvas.GetLeft(flake);
            int y = (int)Canvas.GetTop(flake);

            if (x > Application.Current.MainWindow.Width)
                Canvas.SetLeft(flake, 0);
            if (x < 0)
                Canvas.SetLeft(flake, (int)Application.Current.MainWindow.Width);

            if (Canvas.GetTop(flake) > Application.Current.MainWindow.Height - 25)
            {
                StopPoint here;
                if (Global.stopPoints.ContainsKey(x * y + x))
                {
                    Global.stopPoints.TryGetValue(x * y + x, out here);
                    here.Count++;
                }
                else
                {
                    here = new StopPoint(x,y);
                    Global.stopPoints.Add(here.ID, here);
                }
                if (here.Count < 5)
                    Canvas.SetTop(flake, Canvas.GetTop(flake) - here.Count);
                else
                {
                    flake = null;
                }
                atRest = true;
                return;
            }

            if (readyForChange++ > Global.wobble / Global.speed)
            {
                direction = (float)(_rand.NextDouble() * 3 - 1.75);
                readyForChange = 0;
            }


            Canvas.SetTop(flake, Canvas.GetTop(flake) + Global.speed);
            Canvas.SetLeft(flake, Canvas.GetLeft(flake) + direction);
        }
    }
}