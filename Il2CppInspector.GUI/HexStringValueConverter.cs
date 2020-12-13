// Copyright (c) 2020 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Il2CppInspector.GUI
{
    internal class HexStringValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || targetType != typeof(string))
                return DependencyProperty.UnsetValue;

            return ((ulong) value).ToString("x16");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || targetType != typeof(ulong))
                return DependencyProperty.UnsetValue;

            try {
                return System.Convert.ToUInt64((string) value, 16);
            }
            catch {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}
