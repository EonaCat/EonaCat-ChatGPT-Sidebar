using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EonaCat.ChatGPT.Sidebar.Helpers
{
    public static class WindowHelper
    {
        public static void HideInTabBar(Window window)
        {
            Window fakeWindow = new Window();
            fakeWindow.WindowStyle = WindowStyle.ToolWindow;
            fakeWindow.Top = -100;
            fakeWindow.Left = -100;
            fakeWindow.Width = 1;
            fakeWindow.Height = 1;
            fakeWindow.ShowInTaskbar = false;
            fakeWindow.Show();
            window.Owner = fakeWindow;
            fakeWindow.Hide();
        }
    }
}
