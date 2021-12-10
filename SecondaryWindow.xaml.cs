using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;


namespace WinSnows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SecondaryWindow : Window
    {
        public SecondaryWindow()
        {
            InitializeComponent();
        }


        EllipseBounce[] _particles;
        DispatcherTimer _timer = new DispatcherTimer();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            //particles with Ellipse Geometry
            _particles = new EllipseBounce[300];

            //define area particles can bounce around in
            Rect stage = new Rect(0, 0, this.Width, this.Height);

            //seed particles with random velocity and position
            Random rand = new Random();

            //populate
            for (int i = 0; i < _particles.Length; i++)
            {
                Point pos = new Point((float)(rand.NextDouble() * stage.Width + stage.X), (float)(rand.NextDouble() * stage.Height + stage.Y));
                Point vel = new Point((float)(rand.NextDouble() -0.1), 1.0f);
                _particles[i] = new EllipseBounce(stage, pos, vel, 1);
            }

            //add to particle system - this will draw particles via onrender method
            ParticleSystem ps = new ParticleSystem(_particles);


            //at this element to the grid (assumes we have a Grid in xaml named 'xmalGrid'
            xamlGrid.Children.Add(ps);

            //set up and update function for the particle position
            _timer.Tick += _timer_Tick;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / 60); //update at 60 fps
            _timer.Start();

        }

        void _timer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i].Update();
            }
        }
    }

    /// <summary>
    /// Framework elements that draws particles
    /// </summary>
    public class ParticleSystem : FrameworkElement
    {
        private DrawingGroup _drawingGroup;

        public ParticleSystem(EllipseBounce[] particles)
        {
            _drawingGroup = new DrawingGroup();

            for (int i = 0; i < particles.Length; i++)
            {
                EllipseGeometry eg = particles[i].EllipseGeometry;

                Brush col = Brushes.White;
                col.Freeze();

                GeometryDrawing gd = new GeometryDrawing(col, null, eg);

                _drawingGroup.Children.Add(gd);
            }

        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawDrawing(_drawingGroup);
        }
    }

    /// <summary>
    /// simple class that implements 2d particle movements that bounce from walls
    /// </summary>
    public class SimpleBounce2D
    {
        protected Point _position;
        protected Point _velocity;
        protected Rect _stage;

        public SimpleBounce2D(Rect stage, Point pos, Point vel)
        {
            _stage = stage;

            _position = pos;
            _velocity = vel;
        }

        public double X
        {
            get
            {
                return _position.X;
            }
        }
        public double Y
        {
            get
            {
                return _position.Y;
            }
        }
        public virtual void Update()
        {
            UpdatePosition();
            BoundaryCheck();
        }
        private void UpdatePosition()
        {
            _position.X += _velocity.X;
            _position.Y += _velocity.Y;
        }
        private void BoundaryCheck()
        {
            if (_position.X > _stage.Width + _stage.X)
            {
                _position.X = 0;
            }

            if (_position.X < _stage.X)
            {
                _position.X = _stage.Width;
            }

            if (_position.Y > _stage.Height + _stage.Y)
            {
                _position.Y = 0;
            }

            if (_position.Y < _stage.Y)
            {
                _velocity.Y = -_velocity.Y;
                _position.Y = _stage.Y;
            }
        }
    }


    /// <summary>
    /// extend simplebounce2d to add ellipse geometry and update position in the WPF construct
    /// </summary>
    public class EllipseBounce : SimpleBounce2D
    {
        protected EllipseGeometry _ellipse;

        public EllipseBounce(Rect stage, Point pos, Point vel, float radius)
            : base(stage, pos, vel)
        {
            _ellipse = new EllipseGeometry(pos, radius, radius);
        }

        public EllipseGeometry EllipseGeometry
        {
            get
            {
                return _ellipse;
            }
        }

        public override void Update()
        {
            base.Update();
            _ellipse.Center = _position;
        }
    }
}
