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
    internal class HexStringValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || targetType != typeof(string))
                return DependencyProperty.UnsetValue;

            return value switch {
                ulong n => n.ToString("x16"),
                long n => n.ToString("x16"),
                uint n => n.ToString("x8"),
                int n => n.ToString("x8"),
                ushort n => n.ToString("x4"),
                short n => n.ToString("x4"),
                byte n => n.ToString("x2"),
                _ => throw new NotImplementedException("Unknown number format")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null || !new List<Type> { typeof(ulong), typeof(long), typeof(uint), typeof(int), typeof(ushort), typeof(short), typeof(byte) }.Contains(targetType))
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
