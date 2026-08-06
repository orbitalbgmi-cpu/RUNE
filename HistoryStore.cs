using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RUNE
{
    public sealed class HistoryEntry
    {
        [JsonProperty("sender")]
        public string Sender { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
    }

    public static class HistoryStore
    {
        private static string HistoryPath =>
            Path.Combine(AppContext.BaseDirectory, "data", "history.json");

        public static List<HistoryEntry> LoadAll()
        {
            try
            {
                if (File.Exists(HistoryPath))
                {
                    var json = File.ReadAllText(HistoryPath);
                    var loaded = JsonConvert.DeserializeObject<List<HistoryEntry>>(json);
                    if (loaded != null) return loaded;
                }
            }
            catch { }

            return new List<HistoryEntry>();
        }

        public static void Append(string sender, string text)
        {
            var all = LoadAll();
            all.Add(new HistoryEntry
            {
                Sender = sender,
                Text = text,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            });

            var dir = Path.GetDirectoryName(HistoryPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(HistoryPath, JsonConvert.SerializeObject(all, Formatting.Indented));
        }

        public static void ClearAll()
        {
            if (File.Exists(HistoryPath)) File.Delete(HistoryPath);
        }
    }
}
