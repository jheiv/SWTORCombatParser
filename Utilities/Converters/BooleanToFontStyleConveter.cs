﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SWTORCombatParser.Utilities.Converters
{
    class BooleanToFontStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var wasCrit = (bool)value;
            if (wasCrit)
                return TextDecorations.Underline;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
