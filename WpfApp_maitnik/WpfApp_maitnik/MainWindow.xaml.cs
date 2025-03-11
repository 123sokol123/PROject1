using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.Series;

namespace PendulumSimulation
{
    public partial class MainWindow : Window
    {
        private const double g = 9.81; // Ускорение свободного падения
        private double l; // Длина маятника
        private double fi; // Угол в радианах
        private double omega = 0.0; // Угловая скорость
        private double dampingFactor; // Коэффициент затухания

        private double time = 0.0;
        private const double deltaT = 0.01; // Шаг времени
        private DispatcherTimer timer = new DispatcherTimer();
        private Line pendulumRod;
        private Ellipse pendulumBob;

        private double t0;
        private int Nt = 0;
        private double[] t = new double[10000];
        private double[] fiArray = new double[10000];
        private int iter = 0;

        private bool isPaused = false;

        // Для графика амплитуды
        private PlotModel plotModel;

        public MainWindow()
        {
            InitializeComponent();
            InitializePlot();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += Timer_Tick;
        }

        private void InitializePlot()
        {
            plotModel = new PlotModel { Title = "Амплитуда маятника" };
            var lineSeries = new LineSeries
            {
                Title = "Угол отклонения",
                MarkerType = MarkerType.Circle,
                Color = OxyColors.Blue
            };
            plotModel.Series.Add(lineSeries);
            plotView.Model = plotModel;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Поддержка как точки, так и запятой в числе
                NumberStyles style = NumberStyles.AllowDecimalPoint;
                CultureInfo culture = CultureInfo.InvariantCulture;

                if (!double.TryParse(textBoxLength.Text.Replace(',', '.'), style, culture, out l) || l <= 0)
                    throw new Exception("Длина маятника должна быть положительным числом.");

                if (!double.TryParse(textBoxAngle.Text.Replace(',', '.'), style, culture, out fi))
                    throw new Exception("Угол отклонения должен быть числом.");

                if (!double.TryParse(textBoxDamping.Text.Replace(',', '.'), style, culture, out dampingFactor) || dampingFactor < 0)
                    throw new Exception("Коэффициент затухания не может быть отрицательным.");

                fi = fi * Math.PI / 180.0; // Перевод в радианы
                omega = 0.0;
                time = 0.0;
                iter = 0;
                Nt = 0;
                t0 = 0.0;

                // Удаляем старый график
                var lineSeries = (LineSeries)plotModel.Series[0];
                lineSeries.Points.Clear();
                plotModel.InvalidatePlot(true);

                // Удаляем старые объекты маятника, если они существуют
                MainCanvas.Children.Clear();

                // Инициализация маятника
                pendulumRod = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                MainCanvas.Children.Add(pendulumRod);

                pendulumBob = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Fill = Brushes.Blue
                };
                MainCanvas.Children.Add(pendulumBob);

                timer.Start();

                textBoxHuygens.Text = $"{2.0 * Math.PI * Math.Sqrt(l / g):0.######}";
                textBoxCEI.Text = $"{4.0 * Math.Sqrt(l / g) * cei1(Math.Sin(fi / 2.0)):0.######}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (!isPaused)
            {
                timer.Stop();
                ((Button)sender).Content = "Продолжить";
            }
            else
            {
                timer.Start();
                ((Button)sender).Content = "Пауза";
            }
            isPaused = !isPaused;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            double alpha = -g / l * Math.Sin(fi) - dampingFactor * omega; // Учёт затухания
            omega += alpha * deltaT;
            fi += omega * deltaT;
            time += deltaT;

            // Добавляем значения угла в график
            var lineSeries = (LineSeries)plotModel.Series[0];
            lineSeries.Points.Add(new DataPoint(time, fi * 180 / Math.PI)); // Преобразуем радианы в градусы

            // Ограничиваем количество точек на графике, чтобы избежать перегрузки
            if (lineSeries.Points.Count > 1000)
            {
                lineSeries.Points.RemoveAt(0); // Удаляем старые данные
            }

            plotModel.InvalidatePlot(true); // Обновляем график

            UpdatePendulumPosition();

            // Проверка прохождения через 0
            if (iter < t.Length - 1)
            {
                t[iter] = time;
                fiArray[iter] = fi;
                iter++;

                if (iter > 1 && fiArray[iter - 2] >= 0 && fiArray[iter - 1] < 0)
                {
                    double temp = deltaT * fiArray[iter - 2] / (fiArray[iter - 2] - fiArray[iter - 1]);

                    if (Nt == 0)
                    {
                        t0 = t[iter - 2] + temp;
                    }
                    else
                    {
                        double period = (t[iter - 2] + temp - t0) / Nt;
                        textBoxCalc.Text = $"{period:0.######}";
                    }
                    Nt++;
                }
            }
        }

        private void UpdatePendulumPosition()
        {
            double x0 = 350, y0 = 20; // Точка подвеса
            double x1 = x0 + l * 100 * Math.Sin(fi); // Шарик
            double y1 = y0 + l * 100 * Math.Cos(fi);

            pendulumRod.X1 = x0;
            pendulumRod.Y1 = y0;
            pendulumRod.X2 = x1;
            pendulumRod.Y2 = y1;

            Canvas.SetLeft(pendulumBob, x1 - 10);
            Canvas.SetTop(pendulumBob, y1 - 10);
        }

        private double cei1(double k)
        {
            double t = 1 - k * k;
            double a = (((0.01451196212 * t + 0.03742563713) * t + 0.03590092383) * t + 0.09666344259) * t + 1.38629436112;
            double b = (((0.00441787012 * t + 0.03328355346) * t + 0.06880248576) * t + 0.12498593597) * t + 0.5;
            return a - b * Math.Log(t);
        }
    }
}
