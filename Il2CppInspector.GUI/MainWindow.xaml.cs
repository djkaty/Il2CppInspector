using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Il2CppInspector;

namespace Il2CppInspectorGUI
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
        private async void BtnSelectMetadataFile_OnClick(object sender, RoutedEventArgs e) {
            var app = (App) Application.Current;

            var openFileDialog = new OpenFileDialog {
                Filter = "IL2CPP global metadata file|global-metadata.dat|All files (*.*)|*.*",
                CheckFileExists = true
            };

            btnSelectMetadataFile.Visibility = Visibility.Hidden;

            if (openFileDialog.ShowDialog() == true) {
                // Load the metadata file
                if (await app.LoadMetadataAsync(openFileDialog.FileName)) {
                    // Metadata loaded successfully
                    btnSelectBinaryFile.Visibility = Visibility.Visible;
                    btnBack.Visibility = Visibility.Visible;
                }
                else {
                    MessageBox.Show(this, app.LastException.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnSelectMetadataFile.Visibility = Visibility.Visible;
                }
            }
            else {
                btnSelectMetadataFile.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Select binary file
        /// </summary>
        private async void BtnSelectBinaryFile_OnClick(object sender, RoutedEventArgs e) {
            var app = (App) Application.Current;

            var openFileDialog = new OpenFileDialog {
                Filter = "Binary executable file (*.exe;*.dll;*.so)|*.exe;*.dll;*.so|All files (*.*)|*.*",
                CheckFileExists = true
            };

            btnSelectBinaryFile.Visibility = Visibility.Hidden;
            btnBack.IsEnabled = false;

            if (openFileDialog.ShowDialog() == true) {
                // Load the binary file
                if (await app.LoadBinaryAsync(openFileDialog.FileName)) {
                    // Binary loaded successfully
                    // TODO: Set DataContext
                    // TODO: Format, Endianness, Bits, Arch, GlobalOffset, symbol table size, relocations size, CodeReg, MetaReg
                    rectModalLightBoxBackground.Visibility = Visibility.Hidden;
                }
                else {
                    MessageBox.Show(this, app.LastException.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnSelectBinaryFile.Visibility = Visibility.Visible;
                }
            }
            else {
                btnSelectBinaryFile.Visibility = Visibility.Visible;
            }

            btnBack.IsEnabled = true;
        }

        /// <summary>
        /// Reset binary and metadata files and start again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnBack_OnClick(object sender, RoutedEventArgs e) {
            var app = (App) Application.Current;

            rectModalLightBoxBackground.Visibility = Visibility.Visible;
            gridImageDetails.DataContext = null;
            btnSelectBinaryFile.Visibility = Visibility.Hidden;
            btnBack.Visibility = Visibility.Hidden;
            btnSelectMetadataFile.Visibility = Visibility.Visible;
        }
    }
}
