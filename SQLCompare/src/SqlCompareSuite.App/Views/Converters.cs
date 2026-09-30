using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SqlCompareSuite.App.Views;

/// <summary>
/// bool -> Brush. True => nền highlight khác biệt (#FCE3EC, Redgate diff cell),
/// False => Transparent. Dùng cho cột giá trị động trong Data Compare grid.
/// </summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    // Nền xanh nhạt cho ô khác biệt (khớp RgDiffCell trong App.xaml).
    private static readonly Brush DiffBrush =
        new SolidColorBrush(Color.FromRgb(0xFC, 0xE3, 0xEC));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is bool b && b) ? DiffBrush : Brushes.Transparent;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
