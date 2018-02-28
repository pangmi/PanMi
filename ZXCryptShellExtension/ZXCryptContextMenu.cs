using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

using ZXCryptShared;

namespace ZXCryptShellExtension
{
    /// <summary>
    /// Windows explorer shell context menu
    /// </summary>
    /// <see cref="https://github.com/dwmkerr/sharpshell"/>
    /// <see cref="https://www.codeproject.com/Articles/1035998/NET-Shell-Extensions-Adding-submenus-to-Shell-Cont"/>
    [Guid("47AE0FCB-4246-4D32-AC49-5A6E573BDE55")]
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.AllFiles)]
    [COMServerAssociation(AssociationType.Directory)]
    [COMServerAssociation(AssociationType.Class, @"Directory\Background")]
    public class ZXCryptContextMenu : SharpContextMenu
    {
        private const string APP_NAME = "PanMi";
        private const string APP_EXE = "\\ZXCryptApp.exe";

        private ContextMenuStrip _menu = new ContextMenuStrip();

        private Bitmap _bmpMainMenu;
        private Bitmap _bmpLock;
        private Bitmap _bmpKeyFile;
        private Bitmap _bmpOpen;
        private Bitmap _bmpUnlock;
        private Bitmap _bmpAbout;

        #region Abstract method implementation

        protected override bool CanShowMenu()
        {
            UpdateMenu();
            return true;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            CreateBitmapsForMenuItems();

            int mode = GetMenuOption();

            ToolStripMenuItem mainMenu = new ToolStripMenuItem
            {
                Text = APP_NAME,
                Image = Properties.Resources.panda24,       // _bmpMainMenu,
                ImageScaling = ToolStripItemImageScaling.SizeToFit,
            };

            if ((mode & 2) == 2)
            {
                var subMenu1 = new ToolStripMenuItem
                {
                    Text = "Open",
                    Image = _bmpOpen,
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    Enabled = false
                };
                subMenu1.Click += (sender, args) => ProcessFiles(EncryptionMode.Open);
                if (SelectedItemPaths.Count() == 1)
                {
                    FileAttributes attr = File.GetAttributes(SelectedItemPaths.First());
                    if (!attr.HasFlag(FileAttributes.Directory))
                        subMenu1.Enabled = true;
                }
                mainMenu.DropDownItems.Add(subMenu1);

                var subMenu2 = new ToolStripMenuItem
                {
                    Text = "Decrypt",
                    Image = _bmpUnlock,
                    ImageScaling = ToolStripItemImageScaling.SizeToFit
                };
                subMenu2.Click += (sender, args) => ProcessFiles(EncryptionMode.Decrypt);
                mainMenu.DropDownItems.Add(subMenu2);

            }

            if ((mode & 1) == 1)
            {
                var subMenu1 = new ToolStripMenuItem
                {
                    Text = "Encrypt",
                    Image = _bmpLock,
                    ImageScaling = ToolStripItemImageScaling.SizeToFit
                };
                subMenu1.Click += (sender, args) => ProcessFiles(EncryptionMode.Encrypt);
                mainMenu.DropDownItems.Add(subMenu1);
            }

            if (mode != 0)
                mainMenu.DropDownItems.Add(new ToolStripSeparator());

            var subMenu4 = new ToolStripMenuItem
            {
                Text = "Create Key File",
                Image = _bmpKeyFile,
                ImageScaling = ToolStripItemImageScaling.SizeToFit
            };
            subMenu4.Click += (sender, args) => CreateKeyFile();
            mainMenu.DropDownItems.Add(subMenu4);

            mainMenu.DropDownItems.Add(new ToolStripSeparator());

            var subMenu5 = new ToolStripMenuItem
            {
                Text = "About " + APP_NAME,
                Image = _bmpAbout,
                ImageScaling = ToolStripItemImageScaling.SizeToFit
            };
            subMenu5.Click += (sender, args) => ShowAbout();
            mainMenu.DropDownItems.Add(subMenu5);

            _menu.Items.Clear();
            _menu.Items.Add(mainMenu);

            return _menu;
        }

        #endregion

        #region Private methods

        private void UpdateMenu()
        {
            _menu.Dispose();
            _menu = CreateMenu();    // re-create menu
        }

        private Bitmap ConvertCanvasToBitmap(Canvas canvas)
        {
            if (canvas == null)
                return null;

            canvas.Background = new SolidColorBrush(Colors.LightSteelBlue);

            // reset current transform (in case it is scaled or rotated)
            Transform transform = canvas.LayoutTransform;
            canvas.LayoutTransform = null;

            // needed otherwise the image output is black
            System.Windows.Size size = new System.Windows.Size(canvas.Width, canvas.Height);
            canvas.Measure(size);
            canvas.Arrange(new System.Windows.Rect(size));

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(canvas);

            canvas.LayoutTransform = transform;

            Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                double scaleX = 18.0 / renderBitmap.PixelWidth;
                double scaleY = 18.0 / renderBitmap.PixelHeight;
                BitmapSource bitmap = new TransformedBitmap(renderBitmap, new ScaleTransform(scaleX, scaleY));

                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmap));
                enc.Save(outStream);
                bmp = new Bitmap(outStream);
            }
            return bmp;
        }

        private void CreateBitmapsForMenuItems()
        {
            // Get bitmaps for menu items
            if (_bmpMainMenu == null)
            {
                var canvas = XamlReader.Parse(Properties.Resources.appbar_panda) as Canvas;
                _bmpMainMenu = ConvertCanvasToBitmap(canvas);
            }
            if (_bmpLock == null)
            {
                var canvas = XamlReader.Parse(Properties.Resources.appbar_lock) as Canvas;
                _bmpLock = ConvertCanvasToBitmap(canvas);
            }
            if (_bmpKeyFile == null)
            {
                var canvas = XamlReader.Parse(Properties.Resources.appbar_key) as Canvas;
                _bmpKeyFile = ConvertCanvasToBitmap(canvas);
            }
            if (_bmpAbout == null)
            {
                var canvas = XamlReader.Parse(Properties.Resources.appbar_question) as Canvas;
                _bmpAbout = ConvertCanvasToBitmap(canvas);
            }
            if (_bmpOpen == null)
            {
                var canvas = XamlReader.Parse(Properties.Resources.appbar_book_open_writing) as Canvas;
                _bmpOpen = ConvertCanvasToBitmap(canvas);
            }
            if (_bmpUnlock == null)
            {
                var canvas = XamlReader.Parse(Properties.Resources.appbar_unlock) as Canvas;
                _bmpUnlock = ConvertCanvasToBitmap(canvas);
            }
        }

        /// <summary>
        /// Decide which menu items to display
        /// </summary>
        /// <returns>1: encrypt menu item; 2: decrypt items; 3: both encrypt/decrypt menu items</returns>
        private int GetMenuOption()
        {
            // user right-clicked on the current folder background, so no file was selected. In this case, user can
            // either encrypt or decrypt all files in current folder
            if (SelectedItemPaths.Count() == 0)
                return 3;

            int mode = 0;
            foreach (string file in SelectedItemPaths)
            {
                // when one or more folders were selected, user can either encrypt or decrypt all files in the selected folder(s)
                FileAttributes attr = File.GetAttributes(file);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    mode = 3;
                    break;
                }

                string fileExt = Path.GetExtension(file);
                if (String.Compare(fileExt, ".pxx", StringComparison.CurrentCultureIgnoreCase) == 0)
                    mode |= 2;
                else
                    mode |= 1;

                if (mode == 3)
                    break;
            }

            return mode;
        }

        private void ProcessFiles(EncryptionMode mode)
        {
            try
            {
                string space = " ";
                StringBuilder sb = new StringBuilder();
                sb.Append(mode);
                sb.Append(space);

                if (SelectedItemPaths.Count() > 0)
                {
                    foreach (string file in SelectedItemPaths)
                    {
                        sb.Append("\"");
                        sb.Append(file);
                        sb.Append("\"");
                        sb.Append(space);
                    }
                }
                else
                {
                    // if no file selected (user right-click in the foler background), add the folder
                    sb.Append("\"");
                    sb.Append(FolderPath);
                    sb.Append("\"");
                }

                string dllPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
                string appFullPath = Path.GetDirectoryName(dllPath) + APP_EXE;

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = appFullPath,
                    Arguments = sb.ToString(),
                    UseShellExecute = false
                };
                Process.Start(startInfo);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "PanMi");
            }
        }

        private void CreateKeyFile()
        {
            try
            {
                string dllPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
                string appFullPath = Path.GetDirectoryName(dllPath) + APP_EXE;

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = appFullPath,
                    Arguments = EncryptionMode.CreateKey.ToString(),
                    UseShellExecute = false
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "PanMi");
            }
        }

        private void ShowAbout()
        {
            try
            {
                string dllPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
                string appFullPath = Path.GetDirectoryName(dllPath) + APP_EXE;

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = appFullPath,
                    Arguments = EncryptionMode.About.ToString(),
                    UseShellExecute = false
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, caption: "PanMi");
            }
        }

        #endregion
    }
}
