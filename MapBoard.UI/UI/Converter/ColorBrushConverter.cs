using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Drawing.Color;
using static FzLib.Media.Converter;

namespace MapBoard.UI.Converter
{
    public class ColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (value is Color c)
            {
                return new SolidColorBrush(DrawingColorToMeidaColor(c));
            }
            throw new Exception();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (value is SolidColorBrush b)
            {
                return MediaColorToDrawingColor(b.Color);
            }
            throw new Exception();
        }
    }
}