using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using ZXCryptShared;

namespace ZXCryptApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Windows APIs

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                string[] args = e.Args;

                if (args.Length == 0)
                {
                    MainWindow w = new MainWindow();
                    w.Show();
                    return;
                }

                EncryptionMode mode;
                if (!Enum.TryParse(args[0], out mode))
                {
                    MainWindow w = new MainWindow();
                    w.Show();
                    return;
                }

                string[] fileList = null;
                if (args.Length > 1)
                {
                    fileList = new string[args.Length - 1];
                    Array.Copy(args, 1, fileList, 0, args.Length - 1);
                }

                switch (mode)
                {
                    case EncryptionMode.About:
                        WinAbout wAbout = new WinAbout();
                        SetWindowStartupPosition(wAbout);
                        wAbout.Show();
                        break;
                    case EncryptionMode.CreateKey:
                        WinGenerateKey wCreateKey = new WinGenerateKey();
                        SetWindowStartupPosition(wCreateKey);
                        wCreateKey.Show();
                        break;
                    case EncryptionMode.Open:
                    case EncryptionMode.Encrypt:
                    case EncryptionMode.Decrypt:
                        WinFileEncryption wEnc = new WinFileEncryption(fileList as IEnumerable<string>, mode);
                        SetWindowStartupPosition(wEnc);
                        wEnc.Show();
                        break;
                    default:
                        MainWindow w = new MainWindow();
                        w.Show();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "PanMi");
            }

        }

        /// <summary>
        /// Set this application's window position in the center of the explorer window
        /// </summary>
        /// <param name="w"></param>
        private void SetWindowStartupPosition(Window w)
        {
            IntPtr hwnd = GetForegroundWindow();
            RECT r;
            GetWindowRect(hwnd, out r);

            int height = (int)w.Height;
            int width = (int)w.Width;

            int top = (r.Bottom + r.Top - height) / 2;
            int left = (r.Right + r.Left - width) / 2;

            // adjust in case the window if out of the primary screen
            int scrHeight = (int)SystemParameters.PrimaryScreenHeight;
            int scrWidth = (int)SystemParameters.PrimaryScreenWidth;

            if ((top + height) > scrHeight)
                top = (scrHeight - height) / 2;

            if ((left + width) > scrWidth)
                left = (scrWidth - width) / 2;

            // convert to logic units
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                left = (int)(left * 96 / g.DpiX);
                top = (int)(top * 96 / g.DpiY);
            }

            // set window position
            w.WindowStartupLocation = WindowStartupLocation.Manual;
            w.Top = top;
            w.Left = left;

        }

    }
}
