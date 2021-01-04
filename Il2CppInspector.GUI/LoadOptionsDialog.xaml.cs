/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

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
using Il2CppInspectorGUI;

namespace Il2CppInspector.GUI
{
    /// <summary>
    /// Interaction logic for LoadOptionsDialog.xaml
    /// </summary>
    public partial class LoadOptionsDialog : Window
    {
        public LoadOptionsDialog() {
            InitializeComponent();

            var app = (App) Application.Current;
            DataContext = app.ImageLoadOptions;
        }

        private void okButton_Click(object sender, RoutedEventArgs e) {
            // Closes dialog box automatically
            DialogResult = true;
        }
    }
}
