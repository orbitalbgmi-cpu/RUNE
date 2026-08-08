using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace RUNE.Views
{
    public partial class ChatView : UserControl
    {
        private readonly RUNE.LocalAiModule _ai = new RUNE.LocalAiModule();
        private string _currentSessionId;

        public ChatView()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e) => SendMessage();

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendMessage();
        }

        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            _currentSessionId = null;
            MessageList.Children.Clear();
        }

        public void OpenSession(string sessionId)
        {
            var session = RUNE.HistoryStore.GetSession(sessionId);
            if (session == null) return;

            _currentSessionId = sessionId;
            MessageList.Children.Clear();
            foreach (var msg in session.Messages)
            {
                AddMessage(msg.Sender, msg.Text);
            }
        }

        private string CurrentModelName
        {
            get
            {
                var selected = ModelSelector?.SelectedItem as ComboBoxItem;
                return selected?.Content?.ToString() ?? "Ember";
            }
        }

        private async void SendMessage()
        {
            var text = InputBox.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (_currentSessionId == null)
            {
                var session = RUNE.HistoryStore.CreateSession();
                _currentSessionId = session.Id;
            }

            AddMessage("You", text);
            RUNE.HistoryStore.AppendMessage(_currentSessionId, "You", text);
            InputBox.Clear();

            var modelName = CurrentModelName;
            var deepThink = DeepThinkToggle.IsChecked == true;
            AddMessage(modelName, deepThink ? "thinking carefully..." : "thinking...");

            string reply;
            try
            {
                reply = await _ai.AskAsync(text, modelName, deepThink);
            }
            catch (System.Exception ex)
            {
                reply = "(error: " + ex.Message + ")";
            }

            MessageList.Children.RemoveAt(MessageList.Children.Count - 1);
            AddMessage(modelName, reply);
            RUNE.HistoryStore.AppendMessage(_currentSessionId, modelName, reply);
        }

        private void AddMessage(string sender, string text)
        {
            var thinkingMatch = Regex.Match(text, @"<thinking>(.*?)</?thinking>\s*<answer>(.*?)</?answer>", RegexOptions.Singleline);

            if (thinkingMatch.Success)
            {
                var reasoning = thinkingMatch.Groups[1].Value.Trim();
                var answer = thinkingMatch.Groups[2].Value.Trim();

                MessageList.Children.Add(new TextBlock
                {
                    Text = $"{sender} (reasoning): {reasoning}",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 4),
                    FontStyle = FontStyles.Italic,
                    FontSize = 12,
                    Foreground = (Brush)FindResource("SecondaryTextBrush")
                });

                MessageList.Children.Add(new TextBlock
                {
                    Text = $"{sender}: {answer}",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = (Brush)FindResource("PrimaryTextBrush")
                });
                return;
            }

            MessageList.Children.Add(new TextBlock
            {
                Text = $"{sender}: {text}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = (Brush)FindResource("PrimaryTextBrush")
            });
        }
    }
}
