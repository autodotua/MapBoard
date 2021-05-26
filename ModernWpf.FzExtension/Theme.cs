using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ModernWpf.FzExtension
{
    public static class Theme
    {
        public static void SetTheme(int mode, FrameworkElement element = null)
        {
            ElementTheme theme = mode switch
            {
                0 => ElementTheme.Default,
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            if (element == null)
            {
                foreach (var win in Application.Current.Windows)
                {
                    ThemeManager.SetRequestedTheme(win as Window, theme);
                }
            }
            else
            {
                ThemeManager.SetRequestedTheme(element, theme);
            }
        }

        public static bool SystemUsesLightTheme { get; private set; }
        public static bool AppsUseLightTheme { get; private set; }
    }
}