using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RUNE.Views
{
    public partial class PluginsView : UserControl
    {
        public PluginsView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                WebSearchToggle.IsChecked = App.Config.IsModuleEnabled("web-search");
                RUNE.WebSearchModule.ActivityLogged += OnActivityLogged;
                RefreshLogPlaceholder();
            };
            Unloaded += (s, e) => RUNE.WebSearchModule.ActivityLogged -= OnActivityLogged;
        }

        private void WebSearchToggle_Changed(object sender, RoutedEventArgs e)
        {
            App.Config.SetModuleEnabled("web-search", WebSearchToggle.IsChecked == true);
        }

        private void RefreshLogPlaceholder()
        {
            if (ActivityLog.Children.Count == 0)
            {
                ActivityLog.Children.Add(new TextBlock
                {
                    Text = "No search activity yet.",
                    Foreground = (Brush)FindResource("SecondaryTextBrush"),
                    FontStyle = FontStyles.Italic
                });
            }
        }

        private void OnActivityLogged(string entry)
        {
            Dispatcher.Invoke(() =>
            {
                if (ActivityLog.Children.Count == 1 && ActivityLog.Children[0] is TextBlock tb && tb.FontStyle == FontStyles.Italic)
                {
                    ActivityLog.Children.Clear();
                }

                ActivityLog.Children.Add(new TextBlock
                {
                    Text = entry,
                    Foreground = (Brush)FindResource("PrimaryTextBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 4)
                });
            });
        }
    }
}
