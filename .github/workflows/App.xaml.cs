using System.Windows;
using System.Windows.Media;

namespace RUNE
{
    public partial class App : Application
    {
        public static AppConfig Config { get; private set; }
        public static ModuleManager Modules { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Config = AppConfig.Load();
            var palette = ThemeManager.Load();
            ApplyPalette(palette);

            Modules = new ModuleManager(Config);

            var window = new MainWindow();
            window.Show();
        }

        private void ApplyPalette(ThemePalette palette)
        {
            SetBrush("BackgroundBrush", palette.Background);
            SetBrush("SurfaceBrush", palette.Surface);
            SetBrush("PrimaryTextBrush", palette.PrimaryText);
            SetBrush("SecondaryTextBrush", palette.SecondaryText);
            SetBrush("AccentBrush", palette.Accent);
            SetBrush("BorderBrush", palette.Border);
            SetBrush("SidebarBackgroundBrush", palette.SidebarBackground);
        }

        private void SetBrush(string resourceKey, string hexColor)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                Resources[resourceKey] = new SolidColorBrush(color);
            }
            catch
            {
                // Bad hex value - keep existing default brush.
            }
        }
    }
}
