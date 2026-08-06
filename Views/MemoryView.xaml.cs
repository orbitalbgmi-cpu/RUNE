using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RUNE.Views
{
    public partial class MemoryView : UserControl
    {
        public MemoryView()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadHistory();
        }

        private void LoadHistory()
        {
            HistoryList.Children.Clear();
            var entries = RUNE.HistoryStore.LoadAll();

            if (entries.Count == 0)
            {
                HistoryList.Children.Add(new TextBlock
                {
                    Text = "No conversations saved yet - chat with Ember or NOVA to build up history here.",
                    Foreground = (Brush)FindResource("SecondaryTextBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    FontStyle = FontStyles.Italic
                });
                return;
            }

            foreach (var entry in entries)
            {
                var timestamp = new TextBlock
                {
                    Text = entry.Timestamp,
                    FontSize = 10,
                    Foreground = (Brush)FindResource("SecondaryTextBrush"),
                    Margin = new Thickness(0, 8, 0, 2)
                };
                var message = new TextBlock
                {
                    Text = $"{entry.Sender}: {entry.Text}",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)FindResource("PrimaryTextBrush")
                };
                HistoryList.Children.Add(timestamp);
                HistoryList.Children.Add(message);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            RUNE.HistoryStore.ClearAll();
            LoadHistory();
        }
    }
}
