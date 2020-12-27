/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Windows.Forms.Design;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Linq;
using Il2CppInspector.PluginAPI.V100;
using Il2CppInspector.Reflection;
using Il2CppInspector;
using Ookii.Dialogs.Wpf;

namespace Il2CppInspectorGUI
{ 
    // Class which selects the correct control to display for each plugin option
    public class OptionTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate FilePathTemplate { get; set; }
        public DataTemplate NumberDecimalTemplate { get; set; }
        public DataTemplate NumberHexTemplate { get; set; }
        public DataTemplate BooleanTemplate { get; set; }
        public DataTemplate ChoiceDropdownTemplate { get; set; }
        public DataTemplate ChoiceListTemplate { get; set; }

        // Use some fancy reflection to get the right template property
        // If the plugin option is PluginOptionFooBar and its style enum property is Baz, the template will be FooBarBazTemplate
        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            var option = (IPluginOption) item;
            var style = item.GetType().GetProperty("Style")?.GetValue(item).ToString() ?? string.Empty;
            return (DataTemplate) GetType().GetProperty(option.GetType().Name.Split("`")[0]["PluginOption".Length..] + style + "Template").GetValue(this);
        }
    }

    /// <summary>
    /// Interaction logic for PluginConfigurationDialog.xaml
    /// </summary>
    public partial class PluginConfigurationDialog : Window
    {
        // Item to configure
        public IPlugin Plugin { get; }

        // This helps us find XAML elements withing a DataTemplate
        // Adapted from https://docs.microsoft.com/en-us/dotnet/desktop/wpf/data/how-to-find-datatemplate-generated-elements?view=netframeworkdesktop-4.8
        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++) {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem) {
                    return (childItem) child;
                } else {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        // Adapted from https://stackoverflow.com/a/565560
        // The dependency object is valid if it has no errors and all
        // of its children (that are dependency objects) are error-free.
        private bool IsValid(DependencyObject obj)
            => !Validation.GetHasError(obj) && Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(obj)).All(n => IsValid(VisualTreeHelper.GetChild(obj, n)));

        // Adapted from https://stackoverflow.com/questions/22510428/get-listboxitem-from-listbox
        // In order to force validation with ExceptionValidationRule when the window opens,
        // we need to wait until all of the ListBoxItems are populated, then find the element with the value
        // then force WPF to try to update the source property to see if it raises an exception
        // and cause the DataTriggers to execute.
        // This relies on a 'valueControl' named element existing.
        void OptionsListBoxStatusChanged(object sender, EventArgs e) {
            // Wait for items to be generated
            if (lstOptions.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return;

            // Remove event
            lstOptions.ItemContainerGenerator.StatusChanged -= OptionsListBoxStatusChanged;

            // your items are now generated

            // Adapted from https://stackoverflow.com/a/18008545
            foreach (var item in lstOptions.Items) {
                var listBoxItem = lstOptions.ItemContainerGenerator.ContainerFromItem(item);
                var presenter = FindVisualChild<ContentPresenter>(listBoxItem);

                var dataTemplate = presenter.ContentTemplateSelector.SelectTemplate(item, listBoxItem);
                var boundControl = dataTemplate.FindName("valueControl", presenter);

                // Adapted from https://stackoverflow.com/questions/794370/update-all-bindings-in-usercontrol-at-once
                ((FrameworkElement) boundControl).GetBindingExpression(boundControl switch {
                    TextBox t => TextBox.TextProperty,
                    CheckBox c => CheckBox.IsCheckedProperty,
                    ListBox l => ListBox.SelectedValueProperty,
                    ComboBox m => ComboBox.SelectedValueProperty,
                    TextBlock b => TextBlock.TextProperty,
                    _ => throw new InvalidOperationException("Unknown value control type")
                }).UpdateSource();
            }
        }

        // Initialize configuration dialog window
        public PluginConfigurationDialog(IPlugin plugin) {
            InitializeComponent();
            DataContext = this;
            Plugin = plugin;

            // Validate options once they have loaded
            lstOptions.ItemContainerGenerator.StatusChanged += OptionsListBoxStatusChanged;
        }

        private void okButton_Click(object sender, RoutedEventArgs e) {
            // Close dialog box but call OnClosing first to validate all the options
            DialogResult = true;
        }

        // Select a file path
        private void btnFilePathSelector_Click(object sender, RoutedEventArgs e) {

            var option = (PluginOptionFilePath) ((Button) sender).DataContext;

            string path = null;

            if (option.IsFolder && option.MustExist) {
                var openFolderDialog = new VistaFolderBrowserDialog {
                    SelectedPath = option.Value,
                    Description = option.Description,
                    UseDescriptionForTitle = true
                };
                if (openFolderDialog.ShowDialog() == true) {
                    path = openFolderDialog.SelectedPath;
                }
            } else if (option.MustExist) {
                var openFileDialog = new OpenFileDialog {
                    Title = option.Description,
                    Filter = "All files (*.*)|*.*",
                    FileName = option.Value,
                    CheckFileExists = true,
                    CheckPathExists = true
                };
                if (openFileDialog.ShowDialog() == true) {
                    path = openFileDialog.FileName;
                }
            } else {
                var saveFileDialog = new SaveFileDialog {
                    Title = option.Description,
                    Filter = "All files (*.*)|*.*",
                    FileName = option.Value,
                    CheckFileExists = false,
                    OverwritePrompt = false
                };
                if (saveFileDialog.ShowDialog() == true) {
                    path = saveFileDialog.FileName;
                }
            }

            if (path != null) {
                // This spaghetti saves us from implementing INotifyPropertyChanged on Plugin.Options
                // (we don't want to expose WPF stuff in our SDK)
                // Will break if we change the format of the FilePathDataTemplate too much
                var tb = ((DockPanel) ((Button) sender).Parent).Children.OfType<StackPanel>().First().Children.OfType<TextBox>().First(n => n.Name == "valueControl");
                try {
                    tb.Clear();
                    tb.AppendText(path);
                    tb.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                }
                catch { }
            }
        }

        // Only allow hex characters in hex string
        private void txtHexString_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = !Regex.IsMatch(e.Text, @"[A-Fa-f0-9]");
        }

        // Check options validity before allowing the dialog to close either by clicking OK or the close icon
        private void Window_Closing(object sender, CancelEventArgs e) {
            // Don't allow the window to close if any of the options are invalid
            if (!IsValid(lstOptions)) {
                MessageBox.Show("One or more options are invalid.", "Il2CppInspector Plugin Configuration");
                e.Cancel = true;
                return;
            }

            // Don't allow window to close if the options couldn't be updated
            if (PluginManager.OptionsChanged(Plugin).Error != null)
                e.Cancel = true;
        }
    }
}
