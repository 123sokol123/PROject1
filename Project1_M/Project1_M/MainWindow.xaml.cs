using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Collections.Generic;

namespace PendulumSimulation
{
    public partial class MainWindow : Window
    {
        private const double g = 9.81;
        private double l;
        private double m = 1.0;
        private double theta;
        private double omega = 0.0;
        private double gamma;
        private double dt = 0.01;
        private DispatcherTimer timer;
        private Ellipse pendulumBob;
        private Line pendulumRod;
        private bool isPaused = false;
        private Polyline amplitudeGraph;
        private List<Point> amplitudePoints;
        private Polyline phaseGraph;
        private List<Point> phasePoints;
        private double time = 0.0;
        private double maxTime = 0;

        public MainWindow()
        {
            InitializeComponent();
            StartButton.Click += StartSimulation;
            PauseButton.Click += PauseSimulation;
            StopButton.Click += StopSimulation;
            SetCriticalDampingButton.Click += SetCriticalDamping;

            InitPendulum();

            amplitudeGraph = new Polyline
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2
            };
            AmplitudeCanvas.Children.Add(amplitudeGraph);
            amplitudePoints = new List<Point>();

            phaseGraph = new Polyline
            {
                Stroke = Brushes.Green,
                StrokeThickness = 2
            };
            PhaseCanvas.Children.Add(phaseGraph);  // Используйте PhaseCanvas
            phasePoints = new List<Point>();

            LengthSlider.ValueChanged += (s, e) => UpdateLength();
            DampingSlider.ValueChanged += (s, e) => UpdateDamping();
            AngleInput.TextChanged += AngleInputChanged;
        }

        private void InitPendulum()
        {
            pendulumRod = new Line { Stroke = Brushes.Black, StrokeThickness = 2 };
            pendulumBob = new Ellipse { Width = 20, Height = 20, Fill = Brushes.Red };
            MainCanvas.Children.Add(pendulumRod);
            MainCanvas.Children.Add(pendulumBob);

            LengthValue.Text = LengthSlider.Value.ToString("0.0");
            AngleValue.Text = AngleInput.Text;
            DampingValue.Text = DampingSlider.Value.ToString("0.0");
        }

        private void StartSimulation(object sender, RoutedEventArgs e)
        {
            amplitudePoints.Clear();
            amplitudeGraph.Points.Clear();
            phasePoints.Clear();
            phaseGraph.Points.Clear();
            time = 0.0;

            l = LengthSlider.Value;
            gamma = DampingSlider.Value;

            if (!double.TryParse(AngleInput.Text, out double angle))
            {
                MessageBox.Show("Введите корректное значение угла!");
                return;
            }

            theta = angle * Math.PI / 180.0;
            omega = 0.0;

            if (timer != null)
                timer.Stop();

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            timer.Tick += UpdatePendulum;
            timer.Start();
        }

        private void PauseSimulation(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                if (isPaused)
                {
                    timer.Start();
                    isPaused = false;
                    PauseButton.Content = "Пауза";
                }
                else
                {
                    timer.Stop();
                    isPaused = true;
                    PauseButton.Content = "Возобновить";
                }
            }
        }

        private void StopSimulation(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                omega = 0.0;
                theta = 0.0;
                PauseButton.Content = "Пауза";
                isPaused = false;
            }
        }

        private void UpdatePendulum(object sender, EventArgs e)
        {
            double alpha = -g / l * Math.Sin(theta) - gamma * omega / m;
            omega += alpha * dt;
            theta += omega * dt;

            double x = 150 + l * 100 * Math.Sin(theta);
            double y = 50 + l * 100 * Math.Cos(theta);

            pendulumRod.X1 = 150;
            pendulumRod.Y1 = 50;
            pendulumRod.X2 = x;
            pendulumRod.Y2 = y;

            Canvas.SetLeft(pendulumBob, x - 10);
            Canvas.SetTop(pendulumBob, y - 10);

            UpdateAmplitudeGraph();
            UpdatePhaseGraph();
        }

        private void UpdateLength()
        {
            l = LengthSlider.Value;
            LengthValue.Text = l.ToString("0.0");
        }

        private void UpdateDamping()
        {
            gamma = DampingSlider.Value;
            DampingValue.Text = gamma.ToString("0.0");
        }

        private void AngleInputChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(AngleInput.Text, out double angle))
            {
                AngleValue.Text = angle.ToString("0");
            }
        }

        private void UpdateAmplitudeGraph()
        {
            amplitudePoints.Add(new Point(time, theta * 180 / Math.PI));
            time += dt;

            if (time > maxTime)
                maxTime = time;

            amplitudeGraph.Points.Clear();

            foreach (var point in amplitudePoints)
            {
                double normalizedX = (point.X / maxTime) * (AmplitudeCanvas.Width - 20);
                double normalizedY = 100 - (point.Y / 90) * 50;

                amplitudeGraph.Points.Add(new Point(normalizedX, normalizedY));
            }
        }

        private void UpdatePhaseGraph()
        {
            phasePoints.Add(new Point(theta * 180 / Math.PI, omega));
            phaseGraph.Points.Clear();

            foreach (var point in phasePoints)
            {
                double normalizedX = 150 + point.X * 2;  // Увеличиваем амплитуду по оси X
                double normalizedY = 100 - point.Y * 10; // Увеличиваем амплитуду по оси Y

                phaseGraph.Points.Add(new Point(normalizedX, normalizedY));  // Добавляем точку
            }
        }

        private void SetCriticalDamping(object sender, RoutedEventArgs e)
        {
            double criticalGamma = 2 * Math.Sqrt(g / l);
            DampingSlider.Value = criticalGamma;
            DampingValue.Text = criticalGamma.ToString("0.00");
        }
    }
}
