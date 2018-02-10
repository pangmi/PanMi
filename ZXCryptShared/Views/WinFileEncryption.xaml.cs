using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;

namespace ZXCryptShared
{
    /// <summary>
    /// Interaction logic for WinFileEncryption.xaml
    /// </summary>
    public partial class WinFileEncryption : MetroWindow
    {
        public WinFileEncryption(IEnumerable<string> fileList, EncryptionMode mode)
        {
            InitializeComponent();

            var vm = new MainViewModel(DialogCoordinator.Instance);
            vm.FileList = fileList;
            vm.Mode = mode;

            DataContext = vm;
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            WinAbout dlg = new WinAbout
            {
                Owner = this,
                ShowTitleBar = false,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize
            };
            dlg.ShowDialog();
        }
    }
}
