using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RUNE.Views
{
    public partial class ChatView : UserControl
    {
        private readonly RUNE.LocalAiModule _ai = new RUNE.LocalAiModule();

        public ChatView()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e) => SendMessage();

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendMessage();
        }

        private string CurrentModelName
        {
            get
            {
                var selected = ModelSelector?.SelectedItem as ComboBoxItem;
                var name = selected?.Content?.ToString() ?? "Ember";
                return name.Replace(" (soon)", "");
            }
        }

        private async void SendMessage()
        {
            var text = InputBox.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            AddMessage("You", text);
            RUNE.HistoryStore.Append("You", text);
            InputBox.Clear();

            var modelName = CurrentModelName;
            AddMessage(modelName, "thinking...");

            string reply;
            try
            {
                reply = await _ai.AskAsync(text);
            }
            catch (System.Exception ex)
            {
                reply = "(error: " + ex.Message + ")";
            }

            MessageList.Children.RemoveAt(MessageList.Children.Count - 1);
            AddMessage(modelName, reply);
            RUNE.HistoryStore.Append(modelName, reply);
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
