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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Il2CppInspector.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() {
            InitializeComponent();
        }

        /// <summary>
        /// Select global metadata file
        /// </summary>
        private void BtnSelectMetadataFile_OnClick(object sender, RoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "IL2CPP global metadata file|global-metadata.dat|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true) {
                // openFileDialog.FileName
            }
        }
    }
}
