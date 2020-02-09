// Copyright (c) 2020 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using Il2CppInspector.Reflection;

namespace Il2CppInspectorGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() {
            InitializeComponent();

            // Subscribe to status update events
            ((App) Application.Current).OnStatusUpdate += OnStatusUpdate;
        }

        private void OnStatusUpdate(object sender, string e) => txtBusyStatus.Dispatcher.Invoke(() => txtBusyStatus.Text = e + "...");

        /// <summary>
        /// Select global metadata file
        /// </summary>
        private async void BtnSelectMetadataFile_OnClick(object sender, RoutedEventArgs e) {
            var app = (App) Application.Current;

            var openFileDialog = new OpenFileDialog {
                Filter = "IL2CPP global metadata file|global-metadata.dat|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true) {
                txtBusyStatus.Text = "Processing metadata...";
                areaBusyIndicator.Visibility = Visibility.Visible;
                btnSelectMetadataFile.Visibility = Visibility.Hidden;

                // Load the metadata file
                if (await app.LoadMetadataAsync(openFileDialog.FileName)) {
                    // Metadata loaded successfully
                    btnSelectBinaryFile.Visibility = Visibility.Visible;
                    areaBusyIndicator.Visibility = Visibility.Hidden;
                }
                else {
                    areaBusyIndicator.Visibility = Visibility.Hidden;
                    btnSelectMetadataFile.Visibility = Visibility.Visible;
                    MessageBox.Show(this, app.LastException.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

            if (openFileDialog.ShowDialog() == true) {
                txtBusyStatus.Text = "Processing binary...";
                areaBusyIndicator.Visibility = Visibility.Visible;
                btnSelectBinaryFile.Visibility = Visibility.Hidden;

                // Load the binary file
                if (await app.LoadBinaryAsync(openFileDialog.FileName)) {
                    // Binary loaded successfully
                    areaBusyIndicator.Visibility = Visibility.Hidden;
                    rectModalLightBoxBackground.Visibility = Visibility.Hidden;

                    lstImages.ItemsSource = app.Il2CppModels;
                    lstImages.SelectedIndex = 0;
                }
                else {
                    areaBusyIndicator.Visibility = Visibility.Hidden;
                    btnSelectBinaryFile.Visibility = Visibility.Visible;
                    MessageBox.Show(this, "Something went wrong! " + app.LastException.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Reset binary and metadata files and start again
        /// </summary>
        private void BtnBack_OnClick(object sender, RoutedEventArgs e) {
            rectModalLightBoxBackground.Visibility = Visibility.Visible;
            lstImages.ItemsSource = null;
            btnSelectBinaryFile.Visibility = Visibility.Hidden;
            btnSelectMetadataFile.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// User has selected an image
        /// </summary>
        private void LstImages_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Selection has been removed?
            if (((ListBox) sender).SelectedItem == null) {
                trvNamespaces.ItemsSource = null;
                return;
            }

            // Get selected image
            var model = (Il2CppModel) ((ListBox) sender).SelectedItem;

            // Get namespaces
            var namespaces = model.Assemblies.SelectMany(x => x.DefinedTypes).GroupBy(t => t.Namespace).Select(n => n.Key);

            // Break namespaces down into a tree
            var namespaceTree = deconstructNamespaces(namespaces);

            // Uncheck the default exclusions
            foreach (var exclusion in Constants.DefaultExcludedNamespaces) {
                var parts = exclusion.Split('.');
                CheckboxNode node = null;
                foreach (var part in parts) {
                    node = (node?.Children ?? namespaceTree).FirstOrDefault(c => c.Name == part);
                    if (node == null)
                        break;
                }
                if (node != null)
                    node.IsChecked = false;
            }

            // Populate TreeView with namespace hierarchy
            trvNamespaces.ItemsSource = namespaceTree;
        }

        private IEnumerable<CheckboxNode> deconstructNamespaces(IEnumerable<string> input) {
            if (!input.Any())
                return null;

            var rootAndChildren = input.Select(s => string.IsNullOrEmpty(s)? "<global namespace>" : s)
                                    .GroupBy(n => n.IndexOf(".") != -1 ? n.Substring(0, n.IndexOf(".")) : n).OrderBy(g => g.Key);

            return rootAndChildren.Select(i => new CheckboxNode {Name = i.Key, IsChecked = true, Children = deconstructNamespaces(
                i.Where(s => s.IndexOf(".") != -1).Select(s => s.Substring(s.IndexOf(".") + 1))
                )}).ToList();
        }
    }

    // Replacement for TreeViewItem that includes checkbox state
    internal class CheckboxNode : INotifyPropertyChanged
    {
        private bool? isChecked;
        private string name;
        private IEnumerable<CheckboxNode> children;
        private CheckboxNode parent; // Only needed for ancestor checkbox validation

        public string Name {
            get => name;
            set {
                if (value == name) return;
                name = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<CheckboxNode> Children {
            get => children;
            set {
                if (Equals(value, children)) return;
                children = value;

                // Set parent for each child
                foreach (var child in children)
                    child.parent = this;

                OnPropertyChanged();
            }
        }

        public bool? IsChecked {
            get => isChecked;
            set {
                if (isChecked == value) return;
                isChecked = value;
                OnPropertyChanged();

                // Uncheck all children if needed
                if (isChecked == false && Children != null)
                    foreach (var child in Children)
                        child.IsChecked = false;

                // Process ancestors
                if (isChecked == true && parent != null && parent.isChecked != true)
                    parent.IsChecked = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
