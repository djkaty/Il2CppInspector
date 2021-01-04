/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Il2CppInspector;

namespace Il2CppInspectorGUI
{
    /// <summary>
    /// Interaction logic for PluginManagerDialog.xaml
    /// </summary>
    public partial class PluginManagerDialog : Window
    {
        public PluginManagerDialog() {
            InitializeComponent();
            DataContext = PluginManager.AsInstance;

            // Set default re-order button state
            lstPlugins_SelectionChanged(null, null);
        }

        // Save options whether the user clicked OK or the close icon
        private void Window_Closing(object sender, CancelEventArgs e) {
            ((Il2CppInspectorGUI.App) Application.Current).SaveOptions();
        }

        private void okButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        // Reload list of plugins and reset settings to last save
        private void refreshButton_Click(object sender, RoutedEventArgs e) {
            PluginManager.Reload();
            ((App) Application.Current).LoadOptions();
        }

        // Open configuration for specific plugin
        private void btnConfig_Click(object sender, RoutedEventArgs e) {
            var plugin = (ManagedPlugin) ((Button) sender).DataContext;

            var configDlg = new Il2CppInspectorGUI.PluginConfigurationDialog(plugin);
            configDlg.Owner = this;
            configDlg.ShowDialog();
        }

        // Re-ordering controls
        private void lstPlugins_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var index = lstPlugins.SelectedIndex;

            btnTop.IsEnabled = btnUp.IsEnabled = index > 0;
            btnBottom.IsEnabled = btnDown.IsEnabled = index > -1 && index < lstPlugins.Items.Count - 1;
        }

        private void btnUp_Click(object sender, RoutedEventArgs e) {
            var plugins = PluginManager.AsInstance.ManagedPlugins;

            var index = lstPlugins.SelectedIndex;
            var item = (ManagedPlugin) lstPlugins.SelectedItem;

            plugins.Remove(item);
            plugins.Insert(index - 1, item);
            lstPlugins.SelectedIndex = index - 1;
        }

        private void btnDown_Click(object sender, RoutedEventArgs e) {
            var plugins = PluginManager.AsInstance.ManagedPlugins;

            var index = lstPlugins.SelectedIndex;
            var item = (ManagedPlugin) lstPlugins.SelectedItem;

            plugins.Remove(item);
            plugins.Insert(index + 1, item);
            lstPlugins.SelectedIndex = index + 1;
        }

        private void btnTop_Click(object sender, RoutedEventArgs e) {
            var plugins = PluginManager.AsInstance.ManagedPlugins;

            var index = lstPlugins.SelectedIndex;
            var item = (ManagedPlugin) lstPlugins.SelectedItem;

            plugins.Remove(item);
            plugins.Insert(0, item);
            lstPlugins.SelectedIndex = 0;
        }

        private void btnBottom_Click(object sender, RoutedEventArgs e) {
            var plugins = PluginManager.AsInstance.ManagedPlugins;

            var index = lstPlugins.SelectedIndex;
            var item = (ManagedPlugin) lstPlugins.SelectedItem;

            plugins.Remove(item);
            plugins.Add(item);
            lstPlugins.SelectedIndex = plugins.Count - 1;
        }

        /// Get plugins button
        private void getPluginsButton_Click(object sender, RoutedEventArgs e) {
            Process.Start(new ProcessStartInfo { FileName = @"https://github.com/djkaty/Il2CppInspectorPlugins", UseShellExecute = true });
        }
    }
}
