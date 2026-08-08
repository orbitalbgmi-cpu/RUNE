using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RUNE
{
    public static class FileToolModule
    {
        public static event Action<string> ActivityLogged;

        private static string SandboxFolder =>
            Path.Combine(AppContext.BaseDirectory, "RUNE-Files");

        public static string GetOpenWindowsList()
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var proc in Process.GetProcesses())
                {
                    if (!string.IsNullOrWhiteSpace(proc.MainWindowTitle))
                    {
                        sb.AppendLine($"- {proc.ProcessName}: {proc.MainWindowTitle}");
                    }
                }
                var result = sb.ToString().Trim();
                Log("Read open windows list");
                return string.IsNullOrEmpty(result) ? "(no visible windows found)" : result;
            }
            catch (Exception ex)
            {
                Log("Failed to read windows: " + ex.Message);
                return "(couldn't read open windows)";
            }
        }

        public static string CreateFile(string fileName, string content)
        {
            if (SafetyModule.IsBlocked(fileName) || SafetyModule.IsBlocked(content))
            {
                Log("Blocked file creation request: " + fileName);
                return SafetyModule.RefusalMessage();
            }

            try
            {
                if (!Directory.Exists(SandboxFolder))
                    Directory.CreateDirectory(SandboxFolder);

                // Prevent escaping the sandbox folder (no "..", no absolute paths).
                var safeName = Path.GetFileName(fileName);
                var fullPath = Path.Combine(SandboxFolder, safeName);

                File.WriteAllText(fullPath, content);
                Log("Created file: " + safeName);
                return $"Created {safeName} inside the RUNE-Files folder.";
            }
            catch (Exception ex)
            {
                Log("File creation failed: " + ex.Message);
                return "Couldn't create that file: " + ex.Message;
            }
        }

        public static string ListSandboxFiles()
        {
            try
            {
                if (!Directory.Exists(SandboxFolder))
                    return "(RUNE-Files folder is empty or doesn't exist yet)";

                var files = Directory.GetFiles(SandboxFolder).Select(Path.GetFileName);
                var list = string.Join("\n", files);
                Log("Listed sandbox files");
                return string.IsNullOrEmpty(list) ? "(no files yet)" : list;
            }
            catch (Exception ex)
            {
                return "Couldn't list files: " + ex.Message;
            }
        }

        private static void Log(string message)
        {
            ActivityLogged?.Invoke(DateTime.Now.ToString("HH:mm:ss") + " - " + message);
        }
    }
}
