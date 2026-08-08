using System;
using System.Linq;

namespace RUNE
{
    public static class SafetyModule
    {
        private static readonly string[] BlockedPatterns =
        {
            "how to kill",
        };

        public static bool IsBlocked(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            var lower = message.ToLowerInvariant();
            return BlockedPatterns.Any(pattern => lower.Contains(pattern));
        }

        public static string RefusalMessage()
        {
            return "I can't help with that request - it falls into a category I'm not able to assist with, like killing and harm.";
        }
    }
}
