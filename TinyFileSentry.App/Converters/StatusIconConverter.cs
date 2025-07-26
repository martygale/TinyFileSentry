using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TinyFileSentry.App.Converters;

/// <summary>
/// Converter for transforming string icon key to DrawingImage resource
/// </summary>
public class StatusIconConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string iconKey && !string.IsNullOrEmpty(iconKey))
        {
            try
            {
                // Find resource by key in Application.Current.Resources
                var resource = Application.Current.FindResource(iconKey);
                return resource;
            }
            catch
            {
                // If resource not found, return null (will be empty icon)
                return null;
            }
        }
        
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}