using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RUNE
{
    public static class WebSearchModule
    {
        public static event Action<string> ActivityLogged;
        private static readonly HttpClient _client;

        static WebSearchModule()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        }

        public static async Task<bool> IsInternetAvailableAsync()
        {
            try
            {
                var response = await _client.GetAsync("https://api.duckduckgo.com/?q=test&format=json");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<string> SearchAsync(string query)
        {
            Log("Searching: " + query);

            if (!await IsInternetAvailableAsync())
            {
                Log("Internet is off - search skipped");
                return null;
            }

            try
            {
                var url = "https://api.duckduckgo.com/?q=" + Uri.EscapeDataString(query) + "&format=json&no_html=1";
                var response = await _client.GetStringAsync(url);
                var json = JObject.Parse(response);

                var abstractText = json["AbstractText"]?.ToString();
                var abstractUrl = json["AbstractURL"]?.ToString();

                var cleaned = Sanitize(abstractText);

                if (!string.IsNullOrEmpty(cleaned))
                {
                    Log("Found: " + (string.IsNullOrEmpty(abstractUrl) ? "(no link)" : abstractUrl));
                    return cleaned;
                }

                Log("No usable result for this query");
                return null;
            }
            catch (Exception ex)
            {
                Log("Search failed: " + ex.Message);
                return null;
            }
        }

        private static string Sanitize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (text.Contains("<?php") || text.Contains("<html") || text.Contains("<script") || text.Contains("function("))
                return null;

            text = Regex.Replace(text, "<.*?>", "");
            if (text.Length > 300) text = text.Substring(0, 300);
            return text.Trim();
        }

        private static void Log(string message)
        {
            ActivityLogged?.Invoke(DateTime.Now.ToString("HH:mm:ss") + " - " + message);
        }
    }
}
