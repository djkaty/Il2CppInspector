/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
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
    internal class OptionTemplateSelector : DataTemplateSelector
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

    // Process the 'If' property to enable/disable options
    internal class OptionConditionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || targetType != typeof(bool))
                return DependencyProperty.UnsetValue;

            return ((IPluginOption) value).If();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Interaction logic for PluginConfigurationDialog.xaml
    /// </summary>
    public partial class PluginConfigurationDialog : Window
    {
        // Item to configure
        private ManagedPlugin ManagedPlugin { get; set; }
        public IPlugin Plugin => ManagedPlugin.Plugin;

        // Options when window was opened
        public Dictionary<string, object> OriginalOptions { get; private set; }

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
        private void OptionsListBoxStatusChanged(object sender, EventArgs e) {
            // Wait for items to be generated
            if (lstOptions.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return;

            // Remove event
            lstOptions.ItemContainerGenerator.StatusChanged -= OptionsListBoxStatusChanged;

            // Validate all options
            ValidateAllOptions();
        }

        // Force each ListBoxItem to set its source property
        // This will force errors to appear in the dialog box when exceptions are thrown
        // Only needed when the window first opens or the plugin object changes
        private void ValidateAllOptions() {
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
        public PluginConfigurationDialog(ManagedPlugin plugin) {
            InitializeComponent();
            DataContext = this;
            ManagedPlugin = plugin;

            // Copy current options
            OriginalOptions = plugin.GetOptions();

            // Validate options once they have loaded
            lstOptions.ItemContainerGenerator.StatusChanged += OptionsListBoxStatusChanged;
        }

        // Select a file path
        private void btnFilePathSelector_Click(object sender, RoutedEventArgs e) {

            var option = (PluginOptionFilePath) ((Button) sender).DataContext;

            var filter = string.Join('|', option.AllowedExtensions.Select(e => $"{e.Value} (*.{e.Key.ToLower()})|*.{e.Key.ToLower()}"));

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
                    Filter = filter,
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
                    Filter = filter,
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

        // Reset a plugin's settings
        private void resetButton_Click(object sender, RoutedEventArgs e) {
            // Replace plugin object with a new one (updates ManagedPlugin.Plugin)
            PluginManager.Reset(Plugin);

            // Validate options once they have loaded
            lstOptions.ItemContainerGenerator.StatusChanged += OptionsListBoxStatusChanged;

            // Replace options in ListBox
            lstOptions.ItemsSource = Plugin.Options;
        }

        // Close dialog box but call OnClosing first to validate all the options
        private void okButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        // Close dialog box, reverting changes
        private void cancelButton_Click(object sender, RoutedEventArgs e) {
            // Revert changes
            ManagedPlugin.SetOptions(OriginalOptions, OptionBehaviour.IgnoreInvalid);

            // Replace options in ListBox
            lstOptions.ItemsSource = Plugin.Options;

            DialogResult = false;
        }

        // Check options validity before allowing the dialog to close either by clicking OK or the close icon
        private void Window_Closing(object sender, CancelEventArgs e) {

            // Do nothing if user clicked Cancel
            if (DialogResult == false)
                return;

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

        // Force all the If evaluations on each option to be re-evaluated each time an option is changed
        private void valueControl_Changed(object sender, RoutedEventArgs e) {
            // Ignore changes when listbox is first populated
            if (lstOptions.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return;

            // Update If binding for all options
            foreach (IPluginOption item in lstOptions.Items) {
                var listBoxItem = lstOptions.ItemContainerGenerator.ContainerFromItem(item);
                var presenter = FindVisualChild<ContentPresenter>(listBoxItem);
                var dataTemplate = presenter.ContentTemplateSelector.SelectTemplate(item, listBoxItem);

                if (dataTemplate.FindName("optionPanel", presenter) is FrameworkElement boundControl)
                    boundControl.IsEnabled = item.If();
            }

            // Remove validation errors for disabled options
            ValidateAllOptions();
        }

    }
}
