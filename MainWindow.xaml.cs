using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Input;

namespace KxAmbianceMod
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer snowTimer;
        private Random random = new Random();
        private List<MySnowflake> snowflakes = new List<MySnowflake>();
        private bool isSnowing = true;
        private string configPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KxAmbiance_Theme.txt");

        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public MainWindow() { InitializeComponent(); }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateHotkeys();
            MakeNormal();
            StartSnowTimer();
            LoadSavedTheme();
        }

        private void StartSnowTimer()
        {
            snowTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            snowTimer.Tick += (s, e) =>
            {
                if (!isSnowing) return;
                if (snowflakes.Count < SnowDensitySlider.Value) CreateSnowflake();
                for (int i = snowflakes.Count - 1; i >= 0; i--)
                {
                    var f = snowflakes[i];
                    double t = Canvas.GetTop(f.Shape) + f.Speed;
                    if (t > this.ActualHeight) { t = -10; Canvas.SetLeft(f.Shape, random.NextDouble() * this.ActualWidth); }
                    Canvas.SetTop(f.Shape, t);
                }
            };
            snowTimer.Start();
        }

        private void MenuBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Menü margin bağımlılığından kurtulup serbestçe sürüklenebilir
                this.DragMove();
            }
        }

        private void UpdateHotkeys()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            UnregisterHotKey(hwnd, 9000);
            RegisterHotKey(hwnd, 9000, 0x0000, 0x2D);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(HotKeyHook);
        }

        private IntPtr HotKeyHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312 && wParam.ToInt32() == 9000) ToggleMenu();
            return IntPtr.Zero;
        }

        private void CreateSnowflake()
        {
            var el = new Ellipse { Width = random.Next(2, 4), Height = random.Next(2, 4), Fill = Brushes.White, Opacity = 0.6, IsHitTestVisible = false };
            Canvas.SetLeft(el, random.NextDouble() * SystemParameters.PrimaryScreenWidth); Canvas.SetTop(el, -10);
            EffectCanvas.Children.Add(el); snowflakes.Add(new MySnowflake { Shape = el, Speed = random.NextDouble() * 2 + 1 });
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void ColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            var item = (ComboBoxItem)ColorBox.SelectedItem; string c = item.Content.ToString();
            switch (c)
            {
                case "Purple": AmbianceOverlay.Fill = new SolidColorBrush(Color.FromRgb(128, 0, 128)); break;
                case "Blue": AmbianceOverlay.Fill = new SolidColorBrush(Color.FromRgb(0, 191, 255)); break;
                case "Red": AmbianceOverlay.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0)); break;
                case "Green": AmbianceOverlay.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0)); break;
                default: AmbianceOverlay.Fill = Brushes.Transparent; break;
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (AmbianceOverlay != null) AmbianceOverlay.Opacity = (e.NewValue / 100.0) * 0.15; }
        private void SnowToggle_Checked(object sender, RoutedEventArgs e) => isSnowing = true;
        private void SnowToggle_Unchecked(object sender, RoutedEventArgs e) { isSnowing = false; EffectCanvas.Children.Clear(); snowflakes.Clear(); }

        private void CrossToggle_Checked(object sender, RoutedEventArgs e)
        {
            CrosshairCanvas.Children.Clear();
            double x = SystemParameters.PrimaryScreenWidth / 2;
            double y = SystemParameters.PrimaryScreenHeight / 2;
            Action<double, double, double, double> add = (l, t, w, h) => {
                var r = new Rectangle { Width = w, Height = h, Fill = Brushes.Cyan, Stroke = Brushes.Black, StrokeThickness = 0.5 };
                Canvas.SetLeft(r, l); Canvas.SetTop(r, t); CrosshairCanvas.Children.Add(r);
            };
            add(x - 1, y - 5, 2, 3); add(x - 1, y + 2, 2, 3); add(x - 5, y - 1, 3, 2); add(x + 2, y - 1, 3, 2);
            CrosshairCanvas.Visibility = Visibility.Visible;
        }
        private void CrossToggle_Unchecked(object sender, RoutedEventArgs e) => CrosshairCanvas.Visibility = Visibility.Collapsed;
        private void SelectBgImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog { Filter = "Images|*.jpg;*.png" };
            if (op.ShowDialog() == true) { ApplyTheme(op.FileName); System.IO.File.WriteAllText(configPath, op.FileName); }
        }
        private void ResetBg_Click(object sender, RoutedEventArgs e)
        {
            MenuBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC000000"));
            if (System.IO.File.Exists(configPath)) try { System.IO.File.Delete(configPath); } catch { }
        }
        private void LoadSavedTheme() { if (System.IO.File.Exists(configPath)) { string p = System.IO.File.ReadAllText(configPath); if (System.IO.File.Exists(p)) ApplyTheme(p); } }
        private void ApplyTheme(string p) { try { MenuBorder.Background = new ImageBrush(new BitmapImage(new Uri(p))) { Stretch = Stretch.UniformToFill, Opacity = 0.6 }; } catch { } }
        private void ToggleMenu() { if (MenuBorder.Visibility == Visibility.Visible) { MenuBorder.Visibility = Visibility.Collapsed; MakeTransparent(); } else { MenuBorder.Visibility = Visibility.Visible; MakeNormal(); this.Activate(); } }
        private void MakeTransparent() { var h = new WindowInteropHelper(this).Handle; SetWindowLong(h, -20, GetWindowLong(h, -20) | 0x00000020 | 0x00000080); }
        private void MakeNormal() { var h = new WindowInteropHelper(this).Handle; SetWindowLong(h, -20, GetWindowLong(h, -20) & ~0x00000020); }
    }
    public class MySnowflake { public Ellipse Shape { get; set; } public double Speed { get; set; } }
}