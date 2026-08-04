using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RUNE.Views
{
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e) => SendMessage();

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendMessage();
        }

        private void SendMessage()
        {
            var text = InputBox.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            AddMessage("You", text);
            InputBox.Clear();

            AddMessage("RUNE", "(no AI model connected yet - this is just the UI frame)");
        }

        private void AddMessage(string sender, string text)
        {
            var block = new TextBlock
            {
                Text = $"{sender}: {text}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = (Brush)FindResource("PrimaryTextBrush")
            };
            MessageList.Children.Add(block);
        }
    }
}
