using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RUNE
{
    public static class WebSearchModule
    {
        public static event Action<string> ActivityLogged;
        private static readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

        public static bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = ping.Send("8.8.8.8", 2000);
                    return reply != null && reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<string> SearchAsync(string query)
        {
            Log("Searching: " + query);

            if (!IsInternetAvailable())
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

                if (!string.IsNullOrEmpty(abstractText))
                {
                    Log("Found: " + (string.IsNullOrEmpty(abstractUrl) ? "(no link)" : abstractUrl));
                    return abstractText;
                }

                Log("No direct answer found for this query");
                return null;
            }
            catch (Exception ex)
            {
                Log("Search failed: " + ex.Message);
                return null;
            }
        }

        private static void Log(string message)
        {
            ActivityLogged?.Invoke(DateTime.Now.ToString("HH:mm:ss") + " - " + message);
        }
    }
}
