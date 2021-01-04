// Copyright (c) 2020-2021 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Il2CppInspector.GUI
{
    // Adapted from https://stackoverflow.com/a/37307169 and https://stackoverflow.com/a/28316967
    internal class EqualityConverter : IMultiValueConverter
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length < 2)
                return FalseValue;

            return values[0].Equals(values[1]) ? TrueValue : FalseValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
