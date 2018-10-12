using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Reflection;
using NAudio.Wave;
using NAudio.Midi;
using System.Collections.Concurrent;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace AlgorithmDemonstrate
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //参照http://blog.csdn.net/dlangu0393/article/details/12548731而成
        int customBorderThickness = 5;//需要写上边框阴影区大小以便最大化最小化后还原
        private double[] _Notes = new double[]
        {
            27.5, 29.1353, 30.8677, 32.7032, 34.6479, 36.7081, 38.8909, 41.2035, 43.6536, 46.2493,
            48.9995, 51.913, 55, 58.2705, 61.7354, 65.4064, 69.2957, 73.4162, 77.7817, 82.4069,
            87.3071, 92.4986, 97.9989, 103.826, 110, 116.541, 123.471, 130.813, 138.591, 146.832,
            155.563, 164.814, 174.614, 184.997, 195.998, 207.652, 220, 233.082, 246.942, 261.626,
            277.183, 293.665, 311.127, 329.628, 349.228, 369.994, 391.995, 415.305, 440, 466.164,
            493.883, 523.251, 554.365, 587.33, 622.254, 659.255, 698.456, 739.989, 783.991, 830.609,
            880, 932.328, 987.767, 1046.5, 1108.73, 1174.66, 1244.51, 1318.51, 1396.91, 1479.98,
            1567.98, 1661.22, 1760, 1864.66, 1975.53, 2093, 2217.46, 2349.32, 2489.02, 2637.02,
            2793.83, 2959.96, 3135.96, 3322.44, 3520, 3729.31, 3951.07, 4186.01
        };

        private int[,] _ScaledNotes;

        private static int[] _BarsTemp = null;
        private int[] _Bars;
        private int[] _BarsYO;
        private Rectangle[] _Rectangles;
        private Rectangle[] _Rectangles1;
        private int _BarCount = 40;
        private int _Speed = 990;
        //private string _AlgorithmName;

        private bool _Active = false;
        private SortingAlgorithm<int> _ActiveAlgorithm;
        private bool _Sorted = true;

        private bool _Cancel = false;

        private bool _Active1 = false;
        private SortingAlgorithm<int> _ActiveAlgorithm1;
        private bool _Sorted1 = true;

        private bool _Cancel1 = false;

        private MidiOut _MidiOut;
        private DirectSoundOut _WaveOut;
        private SineWaveProvider32 _WaveProvider;
        private int _MaxFrequency;
        private int _MinFrequency;

        private int _SwapCount;
        private int _CompareCount;
        private int _SwapCount1;
        private int _CompareCount1;

        Brush YORed = new SolidColorBrush(Color.FromRgb(191, 34, 60));
        Brush YORed1 = new SolidColorBrush(Color.FromRgb(191, 34, 61));
        Brush YOBlue = new SolidColorBrush(Color.FromRgb(43, 87, 154));
        Brush YOGreen = new SolidColorBrush(Color.FromRgb(121, 198, 152));
        Brush YOGreen1 = new SolidColorBrush(Color.FromRgb(121, 198, 153));
        Brush YOYellow = new SolidColorBrush(Color.FromRgb(191, 182, 34));

        public MainWindow()
        {
            InitializeComponent();
            #region 无边框窗口相关监听事件
            this.SourceInitialized += MainWindow_SourceInitialized;
            this.StateChanged += MainWindow_StateChanged;
            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
            this.SizeChanged += MainWindow_SizeChanged;
            #endregion

            _WaveProvider = new SineWaveProvider32();
            _WaveProvider.SetWaveFormat(16000, 1);
            _WaveProvider.Amplitude = 0.25f;
            _WaveOut = new DirectSoundOut();
            _WaveOut.Init(_WaveProvider);

            _MidiOut = new MidiOut(0);
        }

        #region 无边框窗口相关代码
        void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Border || e.OriginalSource is TextBlock || e.OriginalSource is Grid || e.OriginalSource is Window)
            {
                WindowInteropHelper wih = new WindowInteropHelper(this);
                Win32.SendMessage(wih.Handle, Win32.WM_NCLBUTTONDOWN, (int)Win32.HitTest.HTCAPTION, 0);
                return;
            }
        }
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                ToggleMaximum.SetValue(Button.StyleProperty, Application.Current.Resources["K_Bbutton"]);
            }
            else
            {
                ToggleMaximum.SetValue(Button.StyleProperty, Application.Current.Resources["Kbutton"]);
            }
        }
        private void ToggleMaximum_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }

        }
        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source == null)
                // Should never be null  
                throw new Exception("Cannot get HwndSource instance.");

            source.AddHook(new HwndSourceHook(this.WndProc));
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            switch (msg)
            {
                case Win32.WM_GETMINMAXINFO: // WM_GETMINMAXINFO message  
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
                case Win32.WM_NCHITTEST: // WM_NCHITTEST message  
                    return WmNCHitTest(lParam, ref handled);
            }

            return IntPtr.Zero;
        }
        /// <summary>  
        /// Corner width used in HitTest  
        /// </summary>  
        private readonly int cornerWidth = 8;

        /// <summary>  
        /// Mouse point used by HitTest  
        /// </summary>  
        private Point mousePoint = new Point();

        private IntPtr WmNCHitTest(IntPtr lParam, ref bool handled)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                return IntPtr.Zero;
            }
            // Update cursor point  
            // The low-order word specifies the x-coordinate of the cursor.  
            // #define GET_X_LPARAM(lp) ((int)(short)LOWORD(lp))  
            this.mousePoint.X = (int)(short)(lParam.ToInt32() & 0xFFFF);
            // The high-order word specifies the y-coordinate of the cursor.  
            // #define GET_Y_LPARAM(lp) ((int)(short)HIWORD(lp))  
            this.mousePoint.Y = (int)(short)(lParam.ToInt32() >> 16);

            // Do hit test  
            handled = true;
            if (Math.Abs(this.mousePoint.Y - this.Top) <= this.cornerWidth
                && Math.Abs(this.mousePoint.X - this.Left) <= this.cornerWidth)
            { // Top-Left  
                return new IntPtr((int)Win32.HitTest.HTTOPLEFT);
            }
            else if (Math.Abs(this.ActualHeight + this.Top - this.mousePoint.Y) <= this.cornerWidth
                && Math.Abs(this.mousePoint.X - this.Left) <= this.cornerWidth)
            { // Bottom-Left  
                return new IntPtr((int)Win32.HitTest.HTBOTTOMLEFT);
            }
            else if (Math.Abs(this.mousePoint.Y - this.Top) <= this.cornerWidth
                && Math.Abs(this.ActualWidth + this.Left - this.mousePoint.X) <= this.cornerWidth)
            { // Top-Right  
                return new IntPtr((int)Win32.HitTest.HTTOPRIGHT);
            }
            else if (Math.Abs(this.ActualWidth + this.Left - this.mousePoint.X) <= this.cornerWidth
                && Math.Abs(this.ActualHeight + this.Top - this.mousePoint.Y) <= this.cornerWidth)
            { // Bottom-Right  
                return new IntPtr((int)Win32.HitTest.HTBOTTOMRIGHT);
            }
            else if (Math.Abs(this.mousePoint.X - this.Left) <= this.customBorderThickness)
            { // Left  
                return new IntPtr((int)Win32.HitTest.HTLEFT);
            }
            else if (Math.Abs(this.ActualWidth + this.Left - this.mousePoint.X) <= this.customBorderThickness)
            { // Right  
                return new IntPtr((int)Win32.HitTest.HTRIGHT);
            }
            else if (Math.Abs(this.mousePoint.Y - this.Top) <= this.customBorderThickness)
            { // Top  
                return new IntPtr((int)Win32.HitTest.HTTOP);
            }
            else if (Math.Abs(this.ActualHeight + this.Top - this.mousePoint.Y) <= this.customBorderThickness)
            { // Bottom  
                return new IntPtr((int)Win32.HitTest.HTBOTTOM);
            }
            else
            {
                handled = false;
                return IntPtr.Zero;
            }
        }
        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            // MINMAXINFO structure  
            Win32.MINMAXINFO mmi = (Win32.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(Win32.MINMAXINFO));

            // Get handle for nearest monitor to this window  
            WindowInteropHelper wih = new WindowInteropHelper(this);
            IntPtr hMonitor = Win32.MonitorFromWindow(wih.Handle, Win32.MONITOR_DEFAULTTONEAREST);

            // Get monitor info  
            Win32.MONITORINFOEX monitorInfo = new Win32.MONITORINFOEX();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            Win32.GetMonitorInfo(new HandleRef(this, hMonitor), monitorInfo);

            // Get HwndSource  
            HwndSource source = HwndSource.FromHwnd(wih.Handle);
            if (source == null)
                // Should never be null  
                throw new Exception("Cannot get HwndSource instance.");
            if (source.CompositionTarget == null)
                // Should never be null  
                throw new Exception("Cannot get HwndTarget instance.");

            // Get transformation matrix  
            Matrix matrix = source.CompositionTarget.TransformFromDevice;

            // Convert working area  
            Win32.RECT workingArea = monitorInfo.rcWork;
            Point dpiIndependentSize =
                matrix.Transform(new Point(
                        workingArea.Right - workingArea.Left,
                        workingArea.Bottom - workingArea.Top
                        ));

            // Convert minimum size  
            Point dpiIndenpendentTrackingSize = matrix.Transform(new Point(
                this.MinWidth,
                this.MinHeight
                ));

            // Set the maximized size of the window  
            mmi.ptMaxSize.x = (int)dpiIndependentSize.X;
            mmi.ptMaxSize.y = (int)dpiIndependentSize.Y;

            // Set the position of the maximized window  
            mmi.ptMaxPosition.x = 0;
            mmi.ptMaxPosition.y = 0;

            // Set the minimum tracking size  
            mmi.ptMinTrackSize.x = (int)dpiIndenpendentTrackingSize.X;
            mmi.ptMinTrackSize.y = (int)dpiIndenpendentTrackingSize.Y;

            Marshal.StructureToPtr(mmi, lParam, true);
        }
        void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                this.BorderThickness = new System.Windows.Thickness(0);
            }
            else
            {
                this.BorderThickness = new System.Windows.Thickness(customBorderThickness);
            }
        }
        #endregion

        private void Menu_HomeButton_Click(object sender, RoutedEventArgs e)//按下主页时
        {
            this.PageContext.Visibility = Visibility.Hidden;
            this.Menu_HomeButton_Bottom.Background = new SolidColorBrush(Colors.White);
            this.Menu_SettingButton_Bottom.Background = new SolidColorBrush(Colors.Transparent);
            this.Menu_AboutButton_Bottom.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void Menu_SettingButton_Click(object sender, RoutedEventArgs e)//按下设置时
        {
            this.PageContext.Source = new Uri("Pages/Introduce.xaml", UriKind.RelativeOrAbsolute);
            this.PageContext.Visibility = Visibility.Visible;
            this.Menu_HomeButton_Bottom.Background = new SolidColorBrush(Colors.Transparent);
            this.Menu_SettingButton_Bottom.Background = new SolidColorBrush(Colors.White);
            this.Menu_AboutButton_Bottom.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void Menu_AboutButton_Click(object sender, RoutedEventArgs e)//按下版本时
        {
            this.PageContext.Source = new Uri("Pages/About.xaml", UriKind.RelativeOrAbsolute);
            this.PageContext.Visibility = Visibility.Visible;
            this.Menu_HomeButton_Bottom.Background = new SolidColorBrush(Colors.Transparent);
            this.Menu_SettingButton_Bottom.Background = new SolidColorBrush(Colors.Transparent);
            this.Menu_AboutButton_Bottom.Background = new SolidColorBrush(Colors.White);
        }

        private void RandomizeBars(bool force)
        {
            if (force) _Sorted = true;
            if (force) _Sorted1 = true;
            RandomizeBars();
        }

        private int _RandomType = 0;
        private void RandomizeBars()
        {
            if (!(_Sorted && _Sorted1)) return;
            int c, i, j, k, h, t, sec;
            _Sorted = false;
            _Sorted1 = false;
            _Bars = new int[_BarCount];
            _BarsYO = new int[_BarCount];
            _Rectangles = new Rectangle[_BarCount];
            _Rectangles1 = new Rectangle[_BarCount];
            _Canvas.Children.Clear();
            _Canvas1.Children.Clear();
            Random rand = new Random();
            for (i = 0; i < _BarCount; i++)
            {
                _Bars[i] = i + 1;
                _Rectangles[i] = new Rectangle();
                _Rectangles1[i] = new Rectangle();
                _Canvas.Children.Add(_Rectangles[i]);
                _Canvas1.Children.Add(_Rectangles1[i]);
            }
            switch (_RandomType)
            {
                case 0:
                    for (i = _BarCount; i > 1; i--)
                    {
                        j = rand.Next(i);
                        t = _Bars[j];
                        _Bars[j] = _Bars[i - 1];
                        _Bars[i - 1] = t;
                    }
                    break;
                case 1:
                    for (i = 0; i < _BarCount; i++)
                    {
                        _Bars[i] = _BarCount - i;
                    }
                    break;
                case 2:
                    sec = Math.Min(10, _BarCount / 5);
                    for (i = 1; i < _BarCount; i += sec)
                    {
                        for (j = Math.Min(i + sec, _BarCount); j > i; j--)
                        {
                            k = rand.Next(i, j);
                            t = _Bars[k];
                            _Bars[k] = _Bars[i - 1];
                            _Bars[i - 1] = t;
                        }
                    }
                    break;
                case 3:
                    sec = Math.Min(10, _BarCount / 10);
                    if (sec == 0)
                    {
                        sec += 1;
                    }
                    h = _BarCount / sec;
                    c = h - 1;
                    for (i = 0; i < _BarCount; i++)
                    {
                        if (i > 10 && i % h == 0)
                            c += h;
                        _Bars[i] = c + 1;
                        if (_Bars[i] > _BarCount)
                        {
                            _Bars[i] = _BarCount - 1;
                        }
                    }
                    for (i = _BarCount; i > 1; i--)
                    {
                        j = rand.Next(i);
                        t = _Bars[j];
                        _Bars[j] = _Bars[i - 1];
                        _Bars[i - 1] = t;
                    }
                    break;
            }
            //_BarsYO = _Bars;
            for (int iui = 0; iui < _BarCount; iui++)
            {
                _BarsYO[iui] = _Bars[iui];
            }
            DrawIt();
            DrawIt1();
        }

        private void Sleep()
        {
            Thread.Sleep(1000 - (int)((_Speed / 2.0 + 50) * 10));
        }

        private bool _PlayOnCompare;
        private bool _PlayOnSwap;

        private void BeginSort(SortingAlgorithm<int> algorithm)
        {
            if (_Active)
                _ActiveAlgorithm.Cancel();
            _Active = true;
            _ActiveAlgorithm = algorithm;
            StopSortButton.Visibility = System.Windows.Visibility.Visible;//
            StartSortButton.Visibility = System.Windows.Visibility.Collapsed;//
            ResetSortButton.IsEnabled = false;
            InputButton.IsEnabled = false;
            _BarCountBox.IsEnabled = false;
            _BarCountBox_text.IsEnabled = false;
            OpenControlgroup.IsEnabled = false;
            ComboBox_Selection.IsEnabled = false;
            canva1select.IsEnabled = false;
            canva1select1.IsEnabled = false;
            _ActiveAlgorithm.Complete += (s2, e2) =>
            {
                _Sorted = true;
                EndSort();

            };
            //RandomizeBars();
            _SwapCount = 0;
            _SwapCountTextBlock.Text = "0";
            _CompareCount = 0;
            _CompareCountTextBlock.Text = "0";
            //_WaveOut.Play();
            _ActiveAlgorithm.ActiveRangeChange += (s, e) =>
            {
                UpdateActiveRange(e.Items);
            };
            _ActiveAlgorithm.ItemsSwapped += (s, e) =>
            {
                _SwapCount++;
                this.Dispatcher.BeginInvoke(new Action(() => { _SwapCountTextBlock.Text = _SwapCount.ToString(); }));
                UpdateRectangleHeight(e.Item1Index, e.Item1);//
                UpdateRectangleHeight(e.Item2Index, e.Item2);//
                UpdateRectangleGreen(e.Item1Index, e.Item2Index);//+++
                if (_PlayOnSwap)
                {
                    PlaySound(e.Item1 - 1);
                    PlaySound(e.Item2 - 1);
                }
            };
            _ActiveAlgorithm.ItemsCompared += (s, e) =>
            {
                _CompareCount++;
                this.Dispatcher.BeginInvoke(new Action(() => { _CompareCountTextBlock.Text = _CompareCount.ToString(); }));
                UpdateIndicator(0, e.Item1Index);
                UpdateIndicator(1, e.Item2Index);
                if (_PlayOnCompare)
                {
                    PlaySound(e.Item1 - 1);
                    PlaySound(e.Item2 - 1);
                }
            };
            _ActiveAlgorithm.ItemUpdated += (s, e) =>
            {
                UpdateRectangleHeight(e.ItemIndex, e.Item);
            };
            foreach (Polygon indicator in _Indicators)
                indicator.Visibility = System.Windows.Visibility.Visible;
            _ActiveAlgorithm.Sort(_Bars);
        }

        private void BeginSort1(SortingAlgorithm<int> yalgorithm)
        {
            if (_Active1)
                _ActiveAlgorithm1.Cancel();
            _Active1 = true;
            _ActiveAlgorithm1 = yalgorithm;
            StopSortButton.Visibility = System.Windows.Visibility.Visible;//
            StartSortButton.Visibility = System.Windows.Visibility.Collapsed;//
            ResetSortButton.IsEnabled = false;
            InputButton.IsEnabled = false;
            _BarCountBox.IsEnabled = false;
            _BarCountBox_text.IsEnabled = false;
            OpenControlgroup.IsEnabled = false;
            ComboBox_Selection.IsEnabled = false;
            canva1select.IsEnabled = false;
            canva1select1.IsEnabled = false;
            _ActiveAlgorithm1.Complete += (s2, e2) =>
            {
                _Sorted1 = true;
                EndSort1();

            };
            //RandomizeBars();
            //_AlgorithmName = algorithm.Name;
            _SwapCount1 = 0;
            _SwapCountTextBlock1.Text = "0";
            _CompareCount1 = 0;
            _CompareCountTextBlock1.Text = "0";
            //_WaveOut.Play();
            _ActiveAlgorithm1.ActiveRangeChange += (s, e) =>
            {
                UpdateActiveRange1(e.Items);
            };
            _ActiveAlgorithm1.ItemsSwapped += (s, e) =>
            {
                _SwapCount1++;
                this.Dispatcher.BeginInvoke(new Action(() => { _SwapCountTextBlock1.Text = _SwapCount1.ToString(); }));
                UpdateRectangleHeight1(e.Item1Index, e.Item1);//
                UpdateRectangleHeight1(e.Item2Index, e.Item2);//
                UpdateRectangleGreen1(e.Item1Index, e.Item2Index);//+++
                if (_PlayOnSwap)
                {
                    PlaySound(e.Item1 - 1);
                    PlaySound(e.Item2 - 1);
                }
            };
            _ActiveAlgorithm1.ItemsCompared += (s, e) =>
            {
                _CompareCount1++;
                this.Dispatcher.BeginInvoke(new Action(() => { _CompareCountTextBlock1.Text = _CompareCount1.ToString(); }));
                UpdateIndicator1(0, e.Item1Index);
                UpdateIndicator1(1, e.Item2Index);
                if (_PlayOnCompare)
                {
                    PlaySound(e.Item1 - 1);
                    PlaySound(e.Item2 - 1);
                }
            };
            _ActiveAlgorithm1.ItemUpdated += (s, e) =>
            {
                UpdateRectangleHeight1(e.ItemIndex, e.Item);
            };
            foreach (Polygon indicator in _Indicators1)
                indicator.Visibility = System.Windows.Visibility.Visible;
            _ActiveAlgorithm1.Sort(_BarsYO);
        }

        private void EndSort()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (_Sorted1 || _Cancel || (OpenControlgroup.IsChecked == false))
                {
                    StopSortButton.Visibility = System.Windows.Visibility.Collapsed;
                    StartSortButton.Visibility = System.Windows.Visibility.Visible;
                    ResetSortButton.IsEnabled = true;
                    InputButton.IsEnabled = true;
                    _BarCountBox.IsEnabled = true;
                    _BarCountBox_text.IsEnabled = true;
                    OpenControlgroup.IsEnabled = true;
                    ComboBox_Selection.IsEnabled = true;
                    canva1select.IsEnabled = true;
                    canva1select1.IsEnabled = true;
                }
                _Active = false;
                foreach (Polygon indicator in _Indicators)
                    indicator.Visibility = System.Windows.Visibility.Hidden;
                DrawIt();
                _Cancel = false;
            }));
            // _WaveOut.Stop();
        }

        private void EndSort1()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (_Sorted || _Cancel1)
                {
                    StopSortButton.Visibility = System.Windows.Visibility.Collapsed;
                    StartSortButton.Visibility = System.Windows.Visibility.Visible;
                    ResetSortButton.IsEnabled = true;
                    InputButton.IsEnabled = true;
                    _BarCountBox.IsEnabled = true;
                    _BarCountBox_text.IsEnabled = true;
                    OpenControlgroup.IsEnabled = true;
                    ComboBox_Selection.IsEnabled = true;
                    canva1select.IsEnabled = true;
                    canva1select1.IsEnabled = true;
                }
                _Active1 = false;
                foreach (Polygon indicator in _Indicators1)
                    indicator.Visibility = System.Windows.Visibility.Hidden;
                DrawIt1();
                _Cancel1 = false;
            }));
            // _WaveOut.Stop();
        }

        private int _SoundType = 1;

        private void PlaySound(params int[] values)
        {
            if (_SoundType == 1)
                PlayMidiSound(values);
        }

        private ConcurrentQueue<Tuple<int, int>> _MidiNotes;
        private int _MidiChannel;
        private int _MaxNotes;
        private void PlayMidiSound(params int[] values)
        {
            if (_MidiNotes == null)
                _MidiNotes = new ConcurrentQueue<Tuple<int, int>>();
            for (int i = 0; i < values.Length; i++)
            {
                try
                {
                    //_MidiOut.Send(MidiMessage.StartNote((int)_ScaledNotes[values[i], 0], 127, _MidiChannel++).RawData);
                    _MidiOut.Send(MidiMessage.StartNote((int)_ScaledNotes[values[i], 0] + 20, 127, 0).RawData);
                    //_MidiNotes.Enqueue(new Tuple<int, int>(_ScaledNotes[values[i], 0], _MidiChannel));
                    if (++_MidiChannel > 16)
                        _MidiChannel = 0;
                    //if (_MidiNotes.Count > _MaxNotes)
                    //{
                    //    Tuple<int, int> note;
                    //    while (!_MidiNotes.TryDequeue(out note)) ;
                    //    _MidiOut.Send(MidiMessage.StopNote(note.Item1, 0, note.Item2).RawData);
                    //}
                }
                catch (Exception)
                {

                }
            }
        }



        private double _BarWidth;
        private double _IndicatorWidth;
        private List<Polygon> _Indicators;
        private List<Polygon> _Indicators1;

        private void UpdateActiveRange(params int[] bars)
        {
            if (_Cancel) return;
            this.Dispatcher.Invoke(new Action(() =>
            {
                for (int i = 0; i < _Bars.Length; i++)
                {
                    if (bars.Contains(i))
                        _Rectangles[i].Fill = YOYellow;
                    else
                        _Rectangles[i].Fill = YOBlue;
                }
            }
            ));
        }

        private void UpdateActiveRange1(params int[] bars)
        {
            if (_Cancel1) return;
            this.Dispatcher.Invoke(new Action(() =>
            {
                for (int i = 0; i < _BarsYO.Length; i++)
                {
                    if (bars.Contains(i))
                        _Rectangles1[i].Fill = YOYellow;
                    else
                        _Rectangles1[i].Fill = YOBlue;
                }
            }
            ));
        }

        /*
        private void UpdateIndicators(params Bar[] bars)
        {
            this.Dispatcher.Invoke(new Action(() =>
                {
                    int i = 0;
                    //float[] frequencies = new float[bars.Length];
                    for (; i < bars.Length; i++)
                    {
                        if (_MidiOut != null)
                        {
                            _MidiOut.Send(MidiMessage.StartNote((int)_ScaledNotes[bars[i].Value - 1, 0] + 20, 127, 0).RawData);
                            if (_ScaledNotes[bars[i].Value - 1, 1] > 0)
                            {
                                _MidiOut.Send(new MidiMessage((int)MidiCommandCode.PitchWheelChange, 0, _ScaledNotes[bars[i].Value -1, 1]).RawData);
                            }
                        }
                        //frequencies[i] = (float)bars[i].Value / (float)_BarCount * (_MaxFrequency - _MinFrequency) + _MinFrequency;
                        //frequencies[i] = (float)_Notes[(int)(((float)bars[i].Value - 1f) / (float)_BarCount * _Notes.Length)];
                        if (_Indicators.Count <= i)
                        {
                            Polygon polygon = new Polygon();
                            polygon.Points = new PointCollection(new Point[] {
                                new Point(0, _IndicatorCanvas.ActualHeight - 1),
                                new Point(_IndicatorWidth / 2, 1),
                                new Point(_IndicatorWidth, _IndicatorCanvas.ActualHeight - 1)});
                            polygon.Fill = Brushes.Red;
                            _Indicators.Add(polygon);
                            _IndicatorCanvas.Children.Add(polygon);
                            Canvas.SetTop(_Indicators[i], 1);
                        }
                        _Indicators[i].Visibility = System.Windows.Visibility.Visible;
                        Canvas.SetLeft(_Indicators[i], bars[i].Index * _BarWidth - 3 + _BarWidth / 2);
                    }
                    for (; i < _Indicators.Count; i++)
                    {
                        _Indicators[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                    //if (_WaveProvider != null && _PlayOnCompare.IsChecked == true)
                    //    _WaveProvider.AddFrequencies(frequencies);
                }
            ));
        }
         * */

        private void UpdateIndicator(int index, int value)
        {
            if (_Cancel) return;
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                //_Indicators[index].Visibility = System.Windows.Visibility.Visible;
                Canvas.SetLeft(_Indicators[index], value * _BarWidth - 3 + _BarWidth / 2);
                if (index == 0)
                {
                    for (int i = 0; i < _BarCount; i++)
                    {
                        if (_Rectangles[i].Fill.ToString() == "#FFBF223C")
                        {
                            _Rectangles[i].Fill = YOBlue;
                        }
                        else if (_Rectangles[i].Fill.ToString() == "#FFBF223D")
                        {
                            _Rectangles[i].Fill = YOYellow;
                        }
                        else if (_Rectangles[i].Fill.ToString() == "#FF79C698")
                        {
                            _Rectangles[i].Fill = YOBlue;
                        }
                        else if (_Rectangles[i].Fill.ToString() == "#FF79C699")
                        {
                            _Rectangles[i].Fill = YOYellow;
                        }
                    }
                    if (_Rectangles[value].Fill.ToString() == "#FF2B579A")
                    {
                        _Rectangles[value].Fill = YORed;
                    }
                    else if (_Rectangles[value].Fill.ToString() == "#FFBFB622")
                    {
                        _Rectangles[value].Fill = YORed1;
                    }
                }
                else
                {
                    if (_Rectangles[value].Fill.ToString() == "#FF2B579A")
                    {
                        _Rectangles[value].Fill = YORed;
                    }
                    else if (_Rectangles[value].Fill.ToString() == "#FFBFB622")
                    {
                        _Rectangles[value].Fill = YORed1;
                    }
                }
            }));
        }

        private void UpdateIndicator1(int index, int value)
        {
            if (_Cancel1) return;
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                //_Indicators1[index].Visibility = System.Windows.Visibility.Visible;
                Canvas.SetLeft(_Indicators1[index], value * _BarWidth - 3 + _BarWidth / 2);
                if (index == 0)
                {
                    for (int i = 0; i < _BarCount; i++)
                    {
                        if (_Rectangles1[i].Fill.ToString() == "#FFBF223C")
                        {
                            _Rectangles1[i].Fill = YOBlue;
                        }
                        else if (_Rectangles1[i].Fill.ToString() == "#FFBF223D")
                        {
                            _Rectangles1[i].Fill = YOYellow;
                        }
                        else if (_Rectangles1[i].Fill.ToString() == "#FF79C698")
                        {
                            _Rectangles1[i].Fill = YOBlue;
                        }
                        else if (_Rectangles1[i].Fill.ToString() == "#FF79C699")
                        {
                            _Rectangles1[i].Fill = YOYellow;
                        }
                    }
                    if (_Rectangles1[value].Fill.ToString() == "#FF2B579A")
                    {
                        _Rectangles1[value].Fill = YORed;
                    }
                    else if (_Rectangles1[value].Fill.ToString() == "#FFBFB622")
                    {
                        _Rectangles1[value].Fill = YORed1;
                    }
                }
                else
                {
                    if (_Rectangles1[value].Fill.ToString() == "#FF2B579A")
                    {
                        _Rectangles1[value].Fill = YORed;
                    }
                    else if (_Rectangles1[value].Fill.ToString() == "#FFBFB622")
                    {
                        _Rectangles1[value].Fill = YORed1;
                    }
                }
            }));
        }

        /*
        private void UpdateRectangleHeights(params Bar[] bars)
        {
            if (_Cancel) return;
            this.Dispatcher.Invoke(new Action(() =>
            {
                float[] frequencies = new float[bars.Length];
                for (int i = 0; i < bars.Length; i++)
                {
                    frequencies[i] = (float)bars[i].Value / (float)_BarCount * (_MaxFrequency - _MinFrequency) + _MinFrequency;
                    _Rectangles[bars[i].Index].Height = _Canvas.ActualHeight * ((double)bars[i].Value / (double)_BarCount);
                    Canvas.SetTop(_Rectangles[bars[i].Index], _Canvas.ActualHeight - _Rectangles[bars[i].Index].Height);
                }
                //if (_WaveProvider != null && _PlayOnSwap.IsChecked == true)
                //    _WaveProvider.AddFrequencies(frequencies);
            }
            ));
        }
         * */

        private void UpdateRectangleGreen(int index1, int index2)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < _BarCount; i++)
                {
                    if (_Rectangles[i].Fill.ToString() == "#FF79C698")
                    {
                        _Rectangles[i].Fill = YOBlue;
                    }
                    else if (_Rectangles[i].Fill.ToString() == "#FF79C699")
                    {
                        _Rectangles[i].Fill = YOYellow;
                    }
                }
                if (_Rectangles[index1].Fill.ToString() == "#FF2B579A")
                {
                    _Rectangles[index1].Fill = YOGreen;
                }
                else if (_Rectangles[index1].Fill.ToString() == "#FFBFB622")
                {
                    _Rectangles[index1].Fill = YOGreen1;
                }
                else if (_Rectangles[index1].Fill.ToString() == "#FFBF223C")
                {
                    _Rectangles[index1].Fill = YOGreen;
                }
                else if (_Rectangles[index1].Fill.ToString() == "#FFBF223D")
                {
                    _Rectangles[index1].Fill = YOGreen1;
                }

                if (_Rectangles[index2].Fill.ToString() == "#FF2B579A")
                {
                    _Rectangles[index2].Fill = YOGreen;
                }
                else if (_Rectangles[index2].Fill.ToString() == "#FFBFB622")
                {
                    _Rectangles[index2].Fill = YOGreen1;
                }
                else if (_Rectangles[index2].Fill.ToString() == "#FFBF223C")
                {
                    _Rectangles[index2].Fill = YOGreen;
                }
                else if (_Rectangles[index2].Fill.ToString() == "#FFBF223D")
                {
                    _Rectangles[index2].Fill = YOGreen1;
                }
            }));
        }

        private void UpdateRectangleGreen1(int index1, int index2)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < _BarCount; i++)
                {
                    if (_Rectangles1[i].Fill.ToString() == "#FF79C698")
                    {
                        _Rectangles1[i].Fill = YOBlue;
                    }
                    else if (_Rectangles1[i].Fill.ToString() == "#FF79C699")
                    {
                        _Rectangles1[i].Fill = YOYellow;
                    }
                }
                if (_Rectangles1[index1].Fill.ToString() == "#FF2B579A")
                {
                    _Rectangles1[index1].Fill = YOGreen;
                }
                else if (_Rectangles1[index1].Fill.ToString() == "#FFBFB622")
                {
                    _Rectangles1[index1].Fill = YOGreen1;
                }
                else if (_Rectangles1[index1].Fill.ToString() == "#FFBF223C")
                {
                    _Rectangles1[index1].Fill = YOGreen;
                }
                else if (_Rectangles1[index1].Fill.ToString() == "#FFBF223D")
                {
                    _Rectangles1[index1].Fill = YOGreen1;
                }

                if (_Rectangles1[index2].Fill.ToString() == "#FF2B579A")
                {
                    _Rectangles1[index2].Fill = YOGreen;
                }
                else if (_Rectangles1[index2].Fill.ToString() == "#FFBFB622")
                {
                    _Rectangles1[index2].Fill = YOGreen1;
                }
                else if (_Rectangles1[index2].Fill.ToString() == "#FFBF223C")
                {
                    _Rectangles1[index2].Fill = YOGreen;
                }
                else if (_Rectangles1[index2].Fill.ToString() == "#FFBF223D")
                {
                    _Rectangles1[index2].Fill = YOGreen1;
                }
            }));
        }


        private void UpdateRectangleHeight(int index, int height)
        {
            if (_Cancel) return;

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                _Rectangles[index].Height = _Canvas.ActualHeight * ((double)height / (double)_Bars.Max());
                Canvas.SetTop(_Rectangles[index], _Canvas.ActualHeight - _Rectangles[index].Height);
            }));
        }

        private void UpdateRectangleHeight1(int index, int height)
        {
            if (_Cancel1) return;

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                _Rectangles1[index].Height = _Canvas1.ActualHeight * ((double)height / (double)_BarsYO.Max());
                Canvas.SetTop(_Rectangles1[index], _Canvas1.ActualHeight - _Rectangles1[index].Height);
            }));
        }

        private void DrawIt(params int[] current)
        {
            DrawIt(new int[] { }, current);
        }

        public void DrawIt(int[] work, params int[] current)
        {
            double margin = 0;
            double drawHeight = _Canvas.ActualHeight;
            double drawWidth = _Canvas.ActualWidth;
            double barWidth = drawWidth / _BarCount;
            double barSpace = barWidth * 0.4;
            if (barSpace < 2.4) barSpace = -1.0;

            for (int i = 0; i < _BarCount; i++)
            {
                double y = _Canvas.ActualHeight - (double)_Bars[i] / (double)_Bars.Max() * drawHeight;
                Rectangle r = _Rectangles[i];
                double width = barWidth - barSpace / 2;
                if (width < 0) width = barWidth;
                this.Dispatcher.Invoke(new Action(() =>
                {
                    r.Fill = YOBlue;
                    r.Height = drawHeight * ((double)_Bars[i] / (double)_Bars.Max());
                    r.Width = width;
                    Canvas.SetLeft(r, barWidth * i + barSpace / 2);
                    Canvas.SetTop(r, y - margin);
                }));
            }
            this.Dispatcher.Invoke(new Action(() => { _Canvas.UpdateLayout(); }));
        }

        private void DrawIt1(params int[] current)
        {
            DrawIt1(new int[] { }, current);
        }

        public void DrawIt1(int[] work, params int[] current)
        {
            double margin = 0;
            double drawHeight = _Canvas1.ActualHeight;
            double drawWidth = _Canvas1.ActualWidth;
            double barWidth = drawWidth / _BarCount;
            double barSpace = barWidth * 0.4;
            if (barSpace < 2.4) barSpace = -1.0;
            for (int i = 0; i < _BarCount; i++)
            {
                double y = _Canvas1.ActualHeight - (double)_BarsYO[i] / (double)_BarsYO.Max() * drawHeight;
                Rectangle r = _Rectangles1[i];
                double width = barWidth - barSpace / 2;
                if (width < 0) width = barWidth;
                this.Dispatcher.Invoke(new Action(() =>
                {
                    r.Fill = YOBlue;
                    r.Height = drawHeight * ((double)_BarsYO[i] / (double)_BarsYO.Max());
                    r.Width = width;
                    Canvas.SetLeft(r, barWidth * i + barSpace / 2);
                    Canvas.SetTop(r, y - margin);
                }));
            }
            this.Dispatcher.Invoke(new Action(() => { _Canvas1.UpdateLayout(); }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this._BarCountBox.Value = 10;
            this._SpeedBox.Value = 50;
            _Indicators = new List<Polygon>();
            for (int i = 0; i < 2; i++)
            {
                Polygon polygon = new Polygon();
                polygon.Points = new PointCollection(new Point[] {
                                    new Point(0, _IndicatorCanvas.ActualHeight - 1),
                                    new Point(_IndicatorWidth / 2, 1),
                                    new Point(_IndicatorWidth, _IndicatorCanvas.ActualHeight - 1)});

                polygon.Fill = YORed;
                polygon.Visibility = System.Windows.Visibility.Hidden;
                _Indicators.Add(polygon);
                _IndicatorCanvas.Children.Add(polygon);
                Canvas.SetTop(_Indicators[i], 1);
            }
            _Indicators1 = new List<Polygon>();
            for (int i = 0; i < 2; i++)
            {
                Polygon polygon = new Polygon();
                polygon.Points = new PointCollection(new Point[] {
                                    new Point(0, _IndicatorCanvas1.ActualHeight - 1),
                                    new Point(_IndicatorWidth / 2, 1),
                                    new Point(_IndicatorWidth, _IndicatorCanvas1.ActualHeight - 1)});
                polygon.Fill = YORed;
                polygon.Visibility = System.Windows.Visibility.Hidden;
                _Indicators1.Add(polygon);
                _IndicatorCanvas1.Children.Add(polygon);
                Canvas.SetTop(_Indicators1[i], 1);
            }
            InitCanvas();
            RandomizeBars(true);
            DrawIt();
            DrawIt1();
        }

        private void _Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InitCanvas();
            DrawIt();
        }

        private void _Canvas1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InitCanvas();
            DrawIt1();
        }

        private void InitCanvas()
        {
            _BarWidth = _Canvas.ActualWidth / _BarCount;
            _IndicatorWidth = 8;
            _BarCountBox.Maximum = (int)_Canvas.ActualWidth;
            if (int.Parse(_BarCountBox_text.Text) >= _BarCountBox.Maximum)
            {
                _BarCountBox_text.Text = (_BarCountBox.Maximum - 1).ToString();
            }
        }

        private Expander _ActiveExpander;
        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (_ActiveExpander != null && _ActiveExpander != (Expander)sender)
            {
                _ActiveExpander.IsExpanded = false;
            }
            _ActiveExpander = (Expander)sender;
        }

        private double _BarSpace;
        private void BarCountBoxChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _BarCount = (int)e.NewValue;
            _BarWidth = _Canvas.ActualWidth / _BarCount;
            _BarSpace = _BarWidth * 0.2;
            RandomizeBars(true);

            double scale = (double)_Notes.Length / (double)(_BarCount - 1);
            _ScaledNotes = new int[_BarCount, 2];
            int z = -1;
            int c = 0;
            for (int i = 0; i < _BarCount; i++)
            {
                int k = (int)(i * scale);
                _ScaledNotes[i, 0] = k;
                _ScaledNotes[i, 1] = 0;
                if (z != k)
                {
                    z = k;
                    if (i > 0 && c > 1)
                    {
                        double step = 0x20d / (double)c;
                        for (int j = 1; j < c; j++)
                        {
                            _ScaledNotes[i - c + j, 1] = (int)(0x3F + j * step);
                        }
                    }
                    c = 1;
                }
                else
                {
                    c++;
                }
            }
        }

        private void SpeedBoxChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SortingAlgorithm<int>.Delay = (int)e.NewValue;
        }

        private void _ConcurrentNotes_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (_WaveProvider != null)
                _WaveProvider.MaxFrequencies = (int)e.NewValue;
            _MaxNotes = (int)e.NewValue;
        }

        private void _MinFrequencyBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            _MinFrequency = (int)e.NewValue;
        }

        private void _MaxFrequencyBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            _MaxFrequency = (int)e.NewValue;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_MidiOut != null)
                _MidiOut.Dispose();
            if (_WaveOut != null)
                _WaveOut.Dispose();
        }

        private void _SO_PlayOnCompare_Checked(object sender, RoutedEventArgs e)
        {
            _PlayOnCompare = _SO_PlayOnCompare.IsChecked == true;
        }

        private void _SO_PlayOnSwap_Checked(object sender, RoutedEventArgs e)
        {
            _PlayOnSwap = _SO_PlayOnSwap.IsChecked == true;
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            _RandomType = ((ComboBox)sender).SelectedIndex;
            RandomizeBars(true);
        }

        //点击开始排序
        private void StartSortButton_Click(object sender, RoutedEventArgs e)
        {
            SortingAlgorithm<int> sortingAlgorithm = SortingAlgorithm<int>.FromName(canva1select.SelectedIndex);
            BeginSort(sortingAlgorithm);
            if (this.OpenControlgroup.IsChecked == true)
            {
                SortingAlgorithm<int> sortingAlgorithm1 = SortingAlgorithm<int>.FromName(canva1select1.SelectedIndex);
                BeginSort1(sortingAlgorithm1);
            }
        }

        //点击停止排序
        private void StopSortButton_Click(object sender, RoutedEventArgs e)
        {
            _Cancel = true;
            _ActiveAlgorithm.Cancel();
            EndSort();
            if (this.OpenControlgroup.IsChecked == true)
            {
                _Cancel1 = true;
                _ActiveAlgorithm1.Cancel();
                EndSort1();
            }
        }

        private void _BarCountBox_text_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.Parse(_BarCountBox_text.Text) > _BarCountBox.Maximum)
            {
                _BarCountBox_text.Text = _BarCountBox.Maximum.ToString();
            }
        }

        private void ResetSortButton_Click(object sender, RoutedEventArgs e)
        {
            RandomizeBars(true);
        }

        private void ComboBox_Selection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _RandomType = ((ComboBox)sender).SelectedIndex;
            RandomizeBars(true);
        }

        private void OpenControlgroup_Checked(object sender, RoutedEventArgs e)
        {
            for (int iui = 0; iui < _BarCount; iui++)
            {
                _BarsYO[iui] = _Bars[iui];
            }
            DrawIt1();
            this._spupon1.Visibility = Visibility.Visible;
            this._boupon1.Visibility = Visibility.Visible;
            this._IndicatorCanvas1.Visibility = Visibility.Visible;
            this.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    Grid.SetRowSpan(this._spupon, 1);
                    Grid.SetRowSpan(this._boupon, 1);
                    Grid.SetRowSpan(this._IndicatorCanvas, 1);
                }));//实时刷新界面
        }

        private void OpenControlgroup_Unchecked(object sender, RoutedEventArgs e)
        {
            this._spupon1.Visibility = Visibility.Hidden;
            this._boupon1.Visibility = Visibility.Hidden;
            this._IndicatorCanvas1.Visibility = Visibility.Hidden;
            this.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    Grid.SetRowSpan(this._spupon, 2);
                    Grid.SetRowSpan(this._boupon, 2);
                    Grid.SetRowSpan(this._IndicatorCanvas, 2);
                }));//实时刷新界面
        }

        private void InputButton_Click(object sender, RoutedEventArgs e)
        {
            InputSource w2 = new InputSource();
            w2.ShowDialog();
            //this.Owner = w2;
            w2.Owner = this;
            //
            _Bars = new int[_BarsTemp.Count()];
            _BarsYO = new int[_BarsTemp.Count()];
            _Sorted = false;
            _Sorted1 = false;
            _BarCount = _BarsTemp.Count();
            this._BarCountBox.Value = _BarsTemp.Count();
            _BarWidth = _Canvas.ActualWidth / _BarCount;
            _BarSpace = _BarWidth * 0.2;
            _Rectangles = new Rectangle[_BarCount];
            _Rectangles1 = new Rectangle[_BarCount];
            _Canvas.Children.Clear();
            _Canvas1.Children.Clear();
            for (int i = 0; i < _BarCount; i++)
            {
                _Bars[i] = _BarsTemp[i];
                _Rectangles[i] = new Rectangle();
                _Rectangles1[i] = new Rectangle();
                _Canvas.Children.Add(_Rectangles[i]);
                _Canvas1.Children.Add(_Rectangles1[i]);
            }
            for (int iui = 0; iui < _BarCount; iui++)
            {
                _BarsYO[iui] = _Bars[iui];
            }
            //创建发声
            double scale = (double)_Notes.Length / (double)(_Bars.Max() - 1);
            _ScaledNotes = new int[_Bars.Max(), 2];
            int z = -1;
            int c = 0;
            for (int i = 0; i < _BarCount; i++)
            {
                int k = (int)(_Bars[i] * scale);
                _ScaledNotes[_Bars[i] - 1, 0] = k;
                _ScaledNotes[_Bars[i] - 1, 1] = 0;
                if (z != k)
                {
                    z = k;
                    if (i > 0 && c > 1)
                    {
                        double step = 0x20d / (double)c;
                        for (int j = 1; j < c; j++)
                        {
                            _ScaledNotes[i - c + j, 1] = (int)(0x3F + j * step);
                        }
                    }
                    c = 1;
                }
                else
                {
                    c++;
                }
            }
            //
            DrawIt();
            DrawIt1();
        }

        public static void InputBars(string[] innum)
        {
            _BarsTemp = new int[innum.Count()];
            for (int i = 0; i < innum.Count(); i++)
            {
                _BarsTemp[i] = int.Parse(innum[i]);
            }

        }

        public static int[] GetInputBars()
        {
            return _BarsTemp;
        }
    }
}
