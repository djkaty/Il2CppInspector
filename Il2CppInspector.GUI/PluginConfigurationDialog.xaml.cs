/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Il2CppInspectorGUI;
using System.Windows.Forms.Design;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Linq;
using Il2CppInspector.PluginAPI.V100;
using static Il2CppInspector.PluginManager;

namespace Il2CppInspector.GUI
{
    // Class which selects the correct control to display for each plugin option
    public class OptionTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate FilePathTemplate { get; set; }
        public DataTemplate NumberTemplate { get; set; }
        public DataTemplate BooleanTemplate { get; set; }
        public DataTemplate ChoiceTemplate { get; set; }

        // Use some fancy reflection to get the right template property
        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            var option = (IPluginOption) item;
            return (DataTemplate) GetType().GetProperty(option.GetType().Name.Split("`")[0]["PluginOption".Length..] + "Template").GetValue(this);
        }
    }

    /// <summary>
    /// Interaction logic for PluginConfigurationDialog.xaml
    /// </summary>
    public partial class PluginConfigurationDialog : Window
    {
        // Item to configure
        public IPlugin Plugin { get; }

        public PluginConfigurationDialog(IPlugin plugin) {
            InitializeComponent();
            DataContext = this;
            Plugin = plugin;
        }

        private void okButton_Click(object sender, RoutedEventArgs e) {
            // Closes dialog box automatically
            DialogResult = true;
        }

        // Select a file path
        private void btnFilePathSelector_Click(object sender, RoutedEventArgs e) {

            var option = (PluginOptionFilePath) ((Button) sender).DataContext;

            var openFileDialog = new OpenFileDialog {
                Title = option.Description,
                Filter = "All files (*.*)|*.*",
                FileName = option.Value,
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true) {
                option.Value = openFileDialog.FileName;

                // This spaghetti saves us from implementing INotifyPropertyChanged on Plugin.Options
                // (we don't want to expose WPF stuff in our SDK)
                // Will break if we change the format of the FilePathDataTemplate too much
                var tb = ((DockPanel) ((Button) sender).Parent).Children.OfType<TextBlock>().First(n => n.Name == "txtFilePathSelector");
                tb.Text = option.Value;
            }
        }

        // Only allow hex characters in hex string
        private void txtHexString_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = !Regex.IsMatch(e.Text, @"[A-Fa-f0-9]");
        }
    }
}
