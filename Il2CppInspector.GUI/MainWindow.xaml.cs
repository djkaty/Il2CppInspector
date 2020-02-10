// Copyright (c) 2020 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
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
using Il2CppInspector.Outputs;
using Il2CppInspector.Reflection;
using Ookii.Dialogs.Wpf;
using Path = System.IO.Path;

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

            // Find Unity paths
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            txtUnityPath.Text = Utils.FindPath($@"{programFiles}\Unity\Hub\Editor\*") ?? "<not set>";
            txtUnityScriptPath.Text = Utils.FindPath($@"{programFiles}\Unity\Hub\Editor\*\Editor\Data\Resources\PackageManager\ProjectTemplates\libcache\com.unity.template.3d-*\ScriptAssemblies") ?? "<not set>";
        }

        /// <summary>
        /// Update the busy indicator message
        /// </summary>
        private void OnStatusUpdate(object sender, string e) => txtBusyStatus.Dispatcher.Invoke(() => txtBusyStatus.Text = e + "...");

        /// <summary>
        /// User clicked on a link
        /// </summary>
        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo {FileName = e.Uri.ToString(), UseShellExecute = true});
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

        /// <summary>
        /// Select Unity editor path
        /// </summary>
        private void BtnUnityPath_OnClick(object sender, RoutedEventArgs e) {
            var openFolderDialog = new VistaFolderBrowserDialog();
            if (txtUnityPath.Text != "<not set>")
                openFolderDialog.SelectedPath = txtUnityPath.Text;
            else {
                openFolderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }

            openFolderDialog.Description = "Select Unity editor path";
            openFolderDialog.UseDescriptionForTitle = true;

            while (openFolderDialog.ShowDialog() == true) {
                if (ValidateUnityPath(openFolderDialog.SelectedPath)) {
                    txtUnityPath.Text = openFolderDialog.SelectedPath;
                    break;
                }
            }
        }

        /// <summary>
        /// Select Unity script assemblies path
        /// </summary>
        private void BtnUnityScriptPath_OnClick(object sender, RoutedEventArgs e) {
            var openFolderDialog = new VistaFolderBrowserDialog();
            if (txtUnityScriptPath.Text != "<not set>")
                openFolderDialog.SelectedPath = txtUnityScriptPath.Text;

            openFolderDialog.Description = "Select Unity script assemblies path";
            openFolderDialog.UseDescriptionForTitle = true;

            while (openFolderDialog.ShowDialog() == true) {
                if (ValidateUnityAssembliesPath(openFolderDialog.SelectedPath)) {
                    txtUnityScriptPath.Text = openFolderDialog.SelectedPath;
                    break;
                }
            }
        }

        private bool ValidateUnityPath(string path) {
            if (File.Exists(path + @"\Editor\Data\Managed\UnityEditor.dll"))
                return true;
            MessageBox.Show(this, "Could not find Unity installation in this folder. Ensure the 'Editor' folder is a child of the selected folder and try again.", "Unity installation not found", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        private bool ValidateUnityAssembliesPath(string path) {
            if (File.Exists(path + @"\UnityEngine.UI.dll"))
                return true;
            MessageBox.Show(this, "Could not find Unity assemblies in this folder. Ensure the selected folder contains UnityEngine.UI.dll and try again.", "Unity assemblies not found", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        /// <summary>
        /// Perform export
        /// </summary>
        private async void BtnExport_OnClick(object sender, RoutedEventArgs e) {
            var model = (Il2CppModel) lstImages.SelectedItem;

            var unityPath = txtUnityPath.Text;
            var unityAssembliesPath = txtUnityScriptPath.Text;

            var sortOrder = rdoSortIndex.IsChecked == true ? "index" :
                            rdoSortName.IsChecked == true ? "name" :
                            "unknown";
            var layout =    rdoLayoutSingle.IsChecked == true? "single" :
                            rdoLayoutAssembly.IsChecked == true? "assembly" :
                            rdoLayoutNamespace.IsChecked == true? "namespace" :
                            rdoLayoutClass.IsChecked == true? "class" :
                            rdoLayoutTree.IsChecked == true? "tree" :
                            "unknown";

            switch (this) {
                // C# prototypes and Visual Studio solution
                case { rdoOutputCSharp: var r, rdoOutputSolution: var s } when r.IsChecked == true || s.IsChecked == true:

                    var createSolution = rdoOutputSolution.IsChecked == true;

                    if (createSolution) {
                        if (!ValidateUnityPath(unityPath))
                            return;
                        if (!ValidateUnityAssembliesPath(unityAssembliesPath))
                            return;
                    }

                    // Get options
                    var excludedNamespaces = constructExcludedNamespaces((IEnumerable<CheckboxNode>) trvNamespaces.ItemsSource);

                    var writer = new CSharpCodeStubs(model) {
                        ExcludedNamespaces = excludedNamespaces.ToList(),
                        SuppressMetadata = cbSuppressMetadata.IsChecked == true,
                        MustCompile = cbMustCompile.IsChecked == true
                    };

                    var flattenHierarchy = cbFlattenHierarchy.IsChecked == true;
                    var separateAssemblyAttributesFiles = cbSeparateAttributes.IsChecked == true;

                    // Determine if we need a filename or a folder - file for single file, folder for everything else
                    var needsFolder = rdoOutputCSharp.IsChecked == false || rdoLayoutSingle.IsChecked == false;

                    var saveFolderDialog = new VistaFolderBrowserDialog {
                        Description = "Select save location",
                        UseDescriptionForTitle = true
                    };
                    var saveFileDialog = new SaveFileDialog {
                        Filter = "C# source files (*.cs)|*.cs|All files (*.*)|*.*",
                        FileName = "types.cs",
                        CheckFileExists = false,
                        OverwritePrompt = true
                    };

                    if (needsFolder && saveFolderDialog.ShowDialog() == false)
                        return;
                    if (!needsFolder && saveFileDialog.ShowDialog() == false)
                        return;


                    txtBusyStatus.Text = createSolution ? "Creating Visual Studio solution..." : "Exporting C# type definitions...";
                    areaBusyIndicator.Visibility = Visibility.Visible;

                    var outPath = needsFolder ? saveFolderDialog.SelectedPath : saveFileDialog.FileName;

                    await Task.Run(() => {
                        if (createSolution)
                            writer.WriteSolution(outPath, unityPath, unityAssembliesPath);
                        else
                            switch (layout, sortOrder) {
                                case ("single", "index"):
                                    writer.WriteSingleFile(outPath, t => t.Index);
                                    break;
                                case ("single", "name"):
                                    writer.WriteSingleFile(outPath, t => t.Name);
                                    break;

                                case ("namespace", "index"):
                                    writer.WriteFilesByNamespace(outPath, t => t.Index, flattenHierarchy);
                                    break;
                                case ("namespace", "name"):
                                    writer.WriteFilesByNamespace(outPath, t => t.Name, flattenHierarchy);
                                    break;

                                case ("assembly", "index"):
                                    writer.WriteFilesByAssembly(outPath, t => t.Index, separateAssemblyAttributesFiles);
                                    break;
                                case ("assembly", "name"):
                                    writer.WriteFilesByAssembly(outPath, t => t.Name, separateAssemblyAttributesFiles);
                                    break;

                                case ("class", _):
                                    writer.WriteFilesByClass(outPath, flattenHierarchy);
                                    break;

                                case ("tree", _):
                                    writer.WriteFilesByClassTree(outPath, separateAssemblyAttributesFiles);
                                    break;
                            }
                    });
                    break;

                // IDA Python script
                case { rdoOutputIDA: var r } when r.IsChecked == true:

                    var scriptSaveFileDialog = new SaveFileDialog {
                        Filter = "Python scripts (*.py)|*.py|All files (*.*)|*.*",
                        FileName = "ida.py",
                        CheckFileExists = false,
                        OverwritePrompt = true
                    };

                    if (scriptSaveFileDialog.ShowDialog() == false)
                        return;

                    var outFile = scriptSaveFileDialog.FileName;

                    txtBusyStatus.Text = "Generating IDAPython script...";
                    areaBusyIndicator.Visibility = Visibility.Visible;

                    await Task.Run(() => {
                        var idaWriter = new IDAPythonScript(model);
                        idaWriter.WriteScriptToFile(outFile);
                    });
                    break;
            }

            areaBusyIndicator.Visibility = Visibility.Hidden;
            MessageBox.Show(this, "Export completed successfully", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private IEnumerable<string> constructExcludedNamespaces(IEnumerable<CheckboxNode> nodes) {
            var ns = new List<string>();

            foreach (var node in nodes) {
                if (node.IsChecked == false)
                    ns.Add(node.FullName == "<global namespace>" ? "" : node.FullName);

                else if (node.Children != null)
                    ns.AddRange(constructExcludedNamespaces(node.Children));
            }
            return ns;
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

        public string FullName => (parent != null ? parent.FullName + "." : "") + Name;

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
