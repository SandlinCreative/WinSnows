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
        #region Debug Options
        public static bool DEBUG_MODE = true;
        public static WindowState WINDOW_STATE = WindowState.Maximized;
        //public static WindowState WINDOW_STATE = WindowState.Normal;
        public static int DEBUG_WINDOW_POS = 250;
        #endregion


        // settings
        public static int flakeSize = 2;
        public static short numFlakes = 15;   // MAX: 32767
        public static short speed = 2;        // MAX: 32767
        public static short flow = 3;         // MAX: 32767
        public static short wobble = 4;       // MAX: 32767
        public static int flakeLimit = -1;
        public static int accumulationLimit = -1;

        //public static Dictionary<int, StopPoint> stopPoints = new Dictionary<int, StopPoint>();
        public static List<StopPoint> stopPoints = new List<StopPoint>();
        public static List<Flake> flakes = new List<Flake>();
        public static List<Rectangle> floorFlakes = new List<Rectangle>();

        public static double w = System.Windows.SystemParameters.PrimaryScreenWidth;
        public static double h = System.Windows.SystemParameters.PrimaryScreenHeight;

        public static int snowFloor = 0; // will be set on window load
        public static int snowWidth = 0; // will be set on window load

        public static Rectangle Stick = new Rectangle();

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


        private Random rand = new Random();
        private DispatcherTimer timer = new DispatcherTimer();
        private Stopwatch stopWatch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();

            //ShowDesktopWindows();
            
            //SecondaryWindow win2 = new SecondaryWindow();
            //win2.Show();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Global.DEBUG_MODE)
            {
                this.Left = Global.w + Global.DEBUG_WINDOW_POS;
                this.WindowState = Global.WINDOW_STATE;
                Global.Stick.Fill = Brushes.Magenta;
                Global.Stick.Width = 800;
                Global.Stick.Height = 1;
                Canvas.SetTop(Global.Stick, Global.snowFloor);
                theCanvas.Children.Add(Global.Stick);
            }

            Global.snowFloor = (int)Application.Current.MainWindow.Height;
            Global.snowWidth = (int)Application.Current.MainWindow.Width;

            DrawSnow();

            timer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / 60); //update at 60 fps
            timer.Tick += UpdateFlakes;
            timer.Tick += UpdateStick;
            timer.Start();
            stopWatch.Start();
        }
        private void UpdateStick(object sender, EventArgs e)
        {
            Canvas.SetTop(Global.Stick, Global.snowFloor);
            Canvas.SetZIndex(Global.Stick, int.MaxValue);
        }
        private void UpdateFlakes(object sender, EventArgs e)
        {
            foreach (Flake flake in Global.flakes)
                flake.UpdateFlake(rand);

            if (Global.flakes.Count > Global.flakeLimit && Global.flakeLimit > 0)
            {
                theCanvas.Children.RemoveRange(0,Global.flakeLimit / 2);
                Global.flakes.RemoveRange(0, Global.flakeLimit / 2);
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
                Global.flakes.Add(flake);
                theCanvas.Children.Add(flake.flake);
            }
        }

    }

    public class StopPoint
    {
        public int X;
        public int Y;
        public int Count;
        public List<Flake> Flakes = new List<Flake>();
        public int HitRange;
        public StopPoint(int _x, int _y, int _hitRange)
        {
            X = _x;
            Y = _y;
            HitRange = _hitRange;
            Count = 0;
         }
        public override string ToString()
        {
            return $"{X}, {Y} ({this.Count})";
        }
        public override bool Equals(object obj)
        {
            StopPoint other = obj as StopPoint;
            if (obj == null || !this.GetType().Equals(obj.GetType()))
                return false;
            else
                return (this.HitRange == other.HitRange);
        }
        public override int GetHashCode()
        {
            return (X << 2) ^ Y;
        }
    }

    public class Flake
    {
        public Rectangle flake;
        private short readyForChange = 0;
        private float direction = 0; // 0:straight, -1:left, 1:right
        private bool atRest = false;
        private int id;
        public Flake(Int32 _startPos)
        {
            flake = new Rectangle();
            flake.Width = Global.flakeSize;
            flake.Height = Global.flakeSize;
            flake.Fill = Brushes.White;
            flake.StrokeThickness = 0;
            Canvas.SetTop(flake, 2);
            Canvas.SetLeft(flake, _startPos);
            Random r = new Random();
            id = r.Next();
        }

        public override bool Equals(object obj)
        {
            Flake other = obj as Flake;
            if (obj == null || !this.GetType().Equals(obj.GetType()))
                return false;
            else
                return ( Canvas.GetTop(this.flake) == Canvas.GetTop(other.flake) &&
                         Canvas.GetLeft(this.flake) == Canvas.GetLeft(other.flake));
        }
        public void UpdateFlake(Random _rand)
        {
            if (flake == null || atRest) return;

            int x = (int)Canvas.GetLeft(flake);
            int y = (int)Canvas.GetTop(flake);
               
            // If snow reaches beyond the screen width, warp to other side
            if (x > Global.snowWidth)
                Canvas.SetLeft(flake, 0);
            if (x < 0)
                Canvas.SetLeft(flake, (int)Global.snowWidth);

            int adjustedX = (int)(x/1);

            // If snow reaches bottom
            if (y >= Global.snowFloor)
            {
                StopPoint here = new StopPoint(x, y, adjustedX);
                if (Global.stopPoints.Contains(here))// (adjustedX, out here))
                {
                    StopPoint newPoint = Global.stopPoints.First<StopPoint>(item => item.HitRange == here.HitRange);
                    if (newPoint.Count < Global.accumulationLimit || Global.accumulationLimit < 0)
                    {
                        newPoint.Count++;
                        Canvas.SetLeft(flake, x);
                        Canvas.SetTop(flake, Canvas.GetTop(flake) - (Global.flakeSize * newPoint.Count));
                    }
                }
                else
                {
                    here = new StopPoint(x,y, adjustedX);
                    Global.stopPoints.Add(here);
                }
                if (Global.accumulationLimit > 0 && here.Count >= Global.accumulationLimit)
                    flake = null;

                
                Global.floorFlakes.Add(flake);
                atRest = true;
                
                if(Global.floorFlakes.Count >= Global.w * 0.2)
                {
                    Global.snowFloor -= Global.flakeSize;
                    Global.floorFlakes.Clear();
                }

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