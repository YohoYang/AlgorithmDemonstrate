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
using System.Text.RegularExpressions;

namespace AlgorithmDemonstrate
{
    /// <summary>
    /// InputSource.xaml 的交互逻辑
    /// </summary>
    public partial class InputSource : Window
    {
        public InputSource()
        {
            InitializeComponent();
            if (MainWindow.GetInputBars() != null)
            {
                int[] temp = MainWindow.GetInputBars();
                string temp1 = "";
                for (int i = 0; i < temp.Count(); i++)
                {
                    temp1 += temp[i];
                    if (i != (temp.Count() - 1))
                    {
                        temp1 += ",";
                    }
                }
                this.InputNumBox.Text = temp1;
            }
            #region 无边框窗口相关监听事件
            this.SourceInitialized += MainWindow_SourceInitialized;
            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
            #endregion

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
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string ystr = this.InputNumBox.Text;
            string[] numInput;
            ystr = ystr.Replace("，", ",");
            this.InputNumBox.Text = ystr;
            var tmpRegex = new Regex("^[0-9,]{1,}$");
            if (tmpRegex.IsMatch(ystr))
            {
                numInput = ystr.Split(',');
                numInput = numInput.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (numInput.Count()>=3)
                {
                    MainWindow.InputBars(numInput);
                    this.Close();
                }
                else
                {
                    this.ShowMessage.Content = "输入的数据量太少";
                }
                //this.ShowMessage.Content = numInput.Count();
            }
            else
            {
                this.ShowMessage.Content = "输入了不规范内容";
            }
            
        }
    }
}
