using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EonaCat.ChatGPT.Sidebar
{
    public static class IgnoreTabBehaviour
    {
        //Setter for use in XAML: this "enables" this behaviour
        public static void SetEnabled(DependencyObject depObj, bool value)
        {
            depObj.SetValue(EnabledProperty, value);
        }

        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached("Enabled", typeof(bool),
            typeof(IgnoreTabBehaviour),
            new FrameworkPropertyMetadata(false, OnEnabledSet));

        static void OnEnabledSet(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
        {
            var uiElement = depObj as UIElement;
            uiElement.PreviewKeyDown +=
              (object _, System.Windows.Input.KeyEventArgs e) =>
              {
                  if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control ||
                      (e.Key == Key.Home || e.Key == Key.End))
                  {
                      e.Handled = true;
                  }
              };
        }
    }
}