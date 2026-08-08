using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LLama;
using LLama.Common;

namespace RUNE
{
    public sealed class LocalAiModule
    {
        private LLamaWeights _model;
        private LLamaContext _context;
        private InteractiveExecutor _executor;
        private string _loadedModelName;

        private static string ModelFolder(string modelName) =>
            Path.Combine(AppContext.BaseDirectory, "models", modelName);

        private static string ModelFilePath(string modelName)
        {
            var folder = ModelFolder(modelName);
            if (!Directory.Exists(folder)) return null;
            var files = Directory.GetFiles(folder, "*.gguf");
            return files.Length > 0 ? files[0] : null;
        }

        private bool EnsureLoaded(string modelName, out string error)
        {
            error = null;
            if (_loadedModelName == modelName && _executor != null) return true;

            var path = ModelFilePath(modelName);
            if (path == null)
            {
                error = $"({modelName} model file not found in models/{modelName}/ - make sure it's copied there)";
                return false;
            }

            _context?.Dispose();
            _model?.Dispose();
            _executor = null;

            var parameters = new ModelParams(path) { ContextSize = 1536 };
            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);
            _executor = new InteractiveExecutor(_context);
            _loadedModelName = modelName;
            return true;
        }

        public async Task<string> AskAsync(string userMessage, string modelName = "Ember", bool deepThink = false)
        {
            if (SafetyModule.IsBlocked(userMessage))
                return SafetyModule.RefusalMessage();

            // Simple direct commands handled without needing the AI model at all - fast and reliable.
            var lower = userMessage.ToLowerInvariant().Trim();
            if (lower.Contains("what") && (lower.Contains("open") || lower.Contains("running")) && (lower.Contains("window") || lower.Contains("app") || lower.Contains("screen")))
            {
                return "Here's what's currently open:\n" + FileToolModule.GetOpenWindowsList();
            }
            if (lower.StartsWith("create a file") || lower.StartsWith("create file") || lower.StartsWith("make a file"))
            {
                var match = Regex.Match(userMessage, @"(?:called|named)\s+([^\s]+\.\w+)", RegexOptions.IgnoreCase);
                var fileName = match.Success ? match.Groups[1].Value : "note.txt";
                return FileToolModule.CreateFile(fileName, "(created by " + modelName + " via RUNE)");
            }
            if (lower.Contains("what files") || lower.Contains("list files") || lower.Contains("list my files"))
            {
                return "Files in RUNE-Files:\n" + FileToolModule.ListSandboxFiles();
            }

            if (!EnsureLoaded(modelName, out var error))
                return error;

            var systemPrompt = $"You are {modelName}, a helpful assistant. Always reply in English, in a friendly, concise way. Never repeat words or phrases. Never claim you searched the web or found something online unless real search results are given to you below.";

            if (deepThink)
            {
                systemPrompt += " Think through this step by step before answering. Put your reasoning inside <thinking></thinking> tags, then put your final answer inside <answer></answer> tags. Keep the reasoning brief.";
            }

            if (App.Config.IsModuleEnabled("web-search"))
            {
                var hasInternet = await WebSearchModule.IsInternetAvailableAsync();
                if (!hasInternet)
                {
                    return "Internet is off, so I can't search right now. Turn on Web Search access in your network settings, or ask me something I might already know.";
                }

                var searchResult = await WebSearchModule.SearchAsync(userMessage);
                if (!string.IsNullOrEmpty(searchResult))
                {
                    systemPrompt += " Real search result: " + searchResult;
                }
                else
                {
                    systemPrompt += " No search result was found. Say so honestly, then answer from your own knowledge if you can.";
                }
            }

            var prompt = $"<|im_start|>system\n{systemPrompt}<|im_end|>\n<|im_start|>user\n{userMessage}<|im_end|>\n<|im_start|>assistant\n";

            var inferenceParams = new InferenceParams
            {
                MaxTokens = deepThink ? 500 : 300,
                RepeatPenalty = 1.5f,
                AntiPrompts = new System.Collections.Generic.List<string> { "<|im_end|>", "<|im_start|>", "You:" }
            };

            var result = "";
            await foreach (var text in _executor.InferAsync(prompt, inferenceParams))
            {
                result += text;
            }

            return CleanUp(result);
        }

        private static string CleanUp(string text)
        {
            text = text.Replace("Ċ", "\n");
            text = text.Replace("<|im_end|>", "");
            text = text.Replace("<|im_start|>", "");
            text = text.Replace("assistant", "");
            return text.Trim();
        }
    }
}
