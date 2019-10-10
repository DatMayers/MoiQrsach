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
using System.Windows.Threading;

namespace Common
{
    public static class Extensions
    {
        public static void Invoke(this DispatcherObject obj, Action act)
        {
            obj.Dispatcher.Invoke(act);
        }

        public static void InvokeAsync(this DispatcherObject obj, Action act)
        {
            obj.Dispatcher.BeginInvoke(act);
        }

        public static void InvokeAsync(this DispatcherObject obj, Action act, DispatcherPriority priority)
        {
            obj.Dispatcher.BeginInvoke(act, priority);
        }
    }
}
