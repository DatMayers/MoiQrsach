using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;

namespace Common.AeroGlass
{
    public class Chrome
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled",
                typeof(Boolean),
                typeof(Chrome),
                new FrameworkPropertyMetadata(OnIsEnabledChanged));

        public static void SetIsEnabled(DependencyObject element, Boolean value)
        {
            element.SetValue(IsEnabledProperty, value);
        }
        public static Boolean GetIsEnabled(DependencyObject element)
        {
            return (Boolean)element.GetValue(IsEnabledProperty);
        }

        public static void OnIsEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if ((bool)args.NewValue == false)
            {
                Window wnd = (Window)obj;
                wnd.Loaded += new RoutedEventHandler(wnd_Loaded);
            }
        }

        static void wnd_Loaded(object sender, RoutedEventArgs e)
        {
            UIElement element = (UIElement)VisualTreeHelper.GetChild((DependencyObject)sender, 0);
            if (element.GetType() == typeof(Border))
            {
                Border border = (Border)element;
                border.CornerRadius = new CornerRadius(5);
                border.BorderBrush = Brushes.Gray;
                border.BorderThickness = new Thickness(2);
            }
        }
    }
}
