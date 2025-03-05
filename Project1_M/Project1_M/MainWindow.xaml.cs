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
        private const double g = 9.81; // Ускорение свободного падения
        private double l; // Длина маятника
        private double m = 1.0; // Масса маятника (не используется в расчетах, но есть в формуле)
        private double theta; // Угол отклонения (в радианах)
        private double omega = 0.0; // Угловая скорость
        private double gamma; // Коэффициент демпфирования
        private double dt = 0.01; // Шаг по времени
        private DispatcherTimer timer; // Таймер для анимации
        private Ellipse pendulumBob; // Шарик маятника
        private Line pendulumRod; // Стержень маятника
        private bool isPaused = false; // Флаг паузы
        private Polyline amplitudeGraph; // График угла отклонения
        private List<Point> amplitudePoints; // Данные для графика отклонения
        private Polyline phaseGraph; // Фазовый портрет (θ, ω)
        private List<Point> phasePoints; // Данные для фазового портрета
        private double time = 0.0; // Текущее время
        private double maxTime = 0; // Максимальное время для нормализации графиков

        public MainWindow()
        {
            InitializeComponent();
            StartButton.Click += StartSimulation;
            PauseButton.Click += PauseSimulation;
            StopButton.Click += StopSimulation;
            SetCriticalDampingButton.Click += SetCriticalDamping;

            InitPendulum(); // Инициализация маятника

            // Инициализация графиков
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
            PhaseCanvas.Children.Add(phaseGraph);
            phasePoints = new List<Point>();

            // Обработчики событий изменения параметров
            LengthSlider.ValueChanged += (s, e) => UpdateLength();
            DampingSlider.ValueChanged += (s, e) => UpdateDamping();
            AngleInput.TextChanged += AngleInputChanged;
        }

        private void InitPendulum()
        {
            // Создание визуальных элементов маятника
            pendulumRod = new Line { Stroke = Brushes.Black, StrokeThickness = 2 };
            pendulumBob = new Ellipse { Width = 20, Height = 20, Fill = Brushes.Red };
            MainCanvas.Children.Add(pendulumRod);
            MainCanvas.Children.Add(pendulumBob);

            // Установка значений параметров
            LengthValue.Text = LengthSlider.Value.ToString("0.0");
            AngleValue.Text = AngleInput.Text;
            DampingValue.Text = DampingSlider.Value.ToString("0.0");
        }

        private void StartSimulation(object sender, RoutedEventArgs e)
        {
            // Очистка графиков перед запуском новой симуляции
            amplitudePoints.Clear();
            amplitudeGraph.Points.Clear();
            phasePoints.Clear();
            phaseGraph.Points.Clear();
            time = 0.0;

            // Получение параметров маятника
            l = LengthSlider.Value;
            gamma = DampingSlider.Value;

            if (!double.TryParse(AngleInput.Text, out double angle))
            {
                MessageBox.Show("Введите корректное значение угла!");
                return;
            }

            theta = angle * Math.PI / 180.0; // Преобразование угла в радианы
            omega = 0.0; // Начальная угловая скорость

            // Перезапуск таймера
            if (timer != null)
                timer.Stop();

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            timer.Tick += UpdatePendulum;
            timer.Start();
        }

        private void PauseSimulation(object sender, RoutedEventArgs e)
        {
            // Пауза или возобновление симуляции
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
            // Остановка симуляции и сброс параметров
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
            // Вычисление углового ускорения с учетом демпфирования
            double alpha = -g / l * Math.Sin(theta) - gamma * omega / m;
            omega += alpha * dt;
            theta += omega * dt;

            // Вычисление координат маятника
            double x = 150 + l * 100 * Math.Sin(theta);
            double y = 50 + l * 100 * Math.Cos(theta);

            // Обновление графики маятника
            pendulumRod.X1 = 150;
            pendulumRod.Y1 = 50;
            pendulumRod.X2 = x;
            pendulumRod.Y2 = y;

            Canvas.SetLeft(pendulumBob, x - 10);
            Canvas.SetTop(pendulumBob, y - 10);

            // Обновление графиков
            UpdateAmplitudeGraph();
            UpdatePhaseGraph();
        }

        private void UpdateLength()
        {
            // Изменение длины маятника
            l = LengthSlider.Value;
            LengthValue.Text = l.ToString("0.0");
        }

        private void UpdateDamping()
        {
            // Изменение коэффициента демпфирования
            gamma = DampingSlider.Value;
            DampingValue.Text = gamma.ToString("0.0");
        }

        private void AngleInputChanged(object sender, TextChangedEventArgs e)
        {
            // Обновление значения угла при вводе пользователем
            if (double.TryParse(AngleInput.Text, out double angle))
            {
                AngleValue.Text = angle.ToString("0");
            }
        }

        private void UpdateAmplitudeGraph()
        {
            // Добавление новой точки в график амплитуды
            amplitudePoints.Add(new Point(time, theta * 180 / Math.PI));
            time += dt;

            if (time > maxTime)
                maxTime = time;

            amplitudeGraph.Points.Clear();

            // Перерисовка графика с нормализацией
            foreach (var point in amplitudePoints)
            {
                double normalizedX = (point.X / maxTime) * (AmplitudeCanvas.Width - 20);
                double normalizedY = 100 - (point.Y / 90) * 50;

                amplitudeGraph.Points.Add(new Point(normalizedX, normalizedY));
            }
        }

        private void UpdatePhaseGraph()
        {
            // Добавление новой точки в фазовый портрет
            phasePoints.Add(new Point(theta * 180 / Math.PI, omega));
            phaseGraph.Points.Clear();

            // Перерисовка фазового портрета
            foreach (var point in phasePoints)
            {
                double normalizedX = 150 + point.X * 2;  // Масштабирование по X
                double normalizedY = 100 - point.Y * 10; // Масштабирование по Y

                phaseGraph.Points.Add(new Point(normalizedX, normalizedY));
            }
        }

        private void SetCriticalDamping(object sender, RoutedEventArgs e)
        {
            // Установка критического демпфирования
            double criticalGamma = 2 * Math.Sqrt(g / l);
            DampingSlider.Value = criticalGamma;
            DampingValue.Text = criticalGamma.ToString("0.00");
        }
    }
}
