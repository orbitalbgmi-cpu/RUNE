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

        private const string OwnerName = "Onyx";

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

            var parameters = new ModelParams(path)
            {
                ContextSize = 1536,
                Threads = 4
            };
            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);
            _executor = new InteractiveExecutor(_context);
            _loadedModelName = modelName;
            return true;
        }

        private async Task<string> GenerateRawAsync(string modelName, string prompt, int maxTokens)
        {
            if (!EnsureLoaded(modelName, out var error)) return error;

            var inferenceParams = new InferenceParams
            {
                MaxTokens = maxTokens,
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

        public async Task<string> AskAsync(string userMessage, string modelName = "Ember", bool deepThink = false)
        {
            if (SafetyModule.IsBlocked(userMessage))
                return SafetyModule.RefusalMessage();

            var lower = userMessage.ToLowerInvariant().Trim();

            if (lower.Contains("what") && (lower.Contains("open") || lower.Contains("running")) && (lower.Contains("window") || lower.Contains("app") || lower.Contains("screen")))
            {
                return "Here's what's currently open:\n" + FileToolModule.GetOpenWindowsList();
            }

            if (lower.Contains("zip") || lower.Contains(" mod") || lower.Contains("compress") || lower.Contains("minecraft"))
            {
                if (lower.Contains("create") || lower.Contains("make") || lower.Contains("convert"))
                {
                    return "I can only create a single plain text-type file (like .txt, .md, .json, .csv, .log) inside the RUNE-Files folder - I can't build zip archives, Minecraft mods, or compiled programs. Want me to create a text file instead?";
                }
            }

            // Fix #1: file creation now actually asks the model to generate real content,
            // instead of writing a placeholder line.
            if ((lower.Contains("create") || lower.Contains("make")) && lower.Contains("file"))
            {
                var extMatch = Regex.Match(userMessage, @"[a-zA-Z0-9_\-]+\.[a-zA-Z]{1,5}");
                var fileName = extMatch.Success ? extMatch.Value : "note.txt";

                var contentPrompt = $"<|im_start|>system\nYou write plain text file content only, no explanations, no markdown fences.<|im_end|>\n<|im_start|>user\n{userMessage}<|im_end|>\n<|im_start|>assistant\n";
                var generatedContent = await GenerateRawAsync(modelName, contentPrompt, 300);

                if (string.IsNullOrWhiteSpace(generatedContent))
                    generatedContent = "(no content was generated)";

                return FileToolModule.CreateFile(fileName, generatedContent);
            }

            if (lower.Contains("what files") || lower.Contains("list files") || lower.Contains("list my files"))
            {
                return "Files in RUNE-Files:\n" + FileToolModule.ListSandboxFiles();
            }

            if (lower.Contains("who made you") || lower.Contains("who created you") || lower.Contains("who built you") || lower.Contains("who is your creator"))
            {
                return $"I was created by {OwnerName}, as part of the RUNE project.";
            }

            var systemPrompt = $"You are {modelName}, a helpful assistant created by {OwnerName} as part of the RUNE project. Always reply in English, in a friendly, concise way. Never repeat words or phrases. Never claim you searched the web or found something online unless real search results are given to you below. If asked who made you, say {OwnerName}. You cannot create zip files, mods, or compiled programs - only plain text files.";

            if (deepThink)
            {
                systemPrompt += " Think through this step by step before answering. Put your reasoning inside <thinking></thinking> tags, then your final answer inside <answer></answer> tags. Keep the reasoning brief.";
            }

            // Fix #3: clearly mark in the reply whether a real search actually happened.
            var searchUsed = false;
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
                    searchUsed = true;
                }
                else
                {
                    systemPrompt += " No search result was found. Say so honestly, then answer from your own knowledge if you can.";
                }
            }

            var prompt = $"<|im_start|>system\n{systemPrompt}<|im_end|>\n<|im_start|>user\n{userMessage}<|im_end|>\n<|im_start|>assistant\n";
            var answer = await GenerateRawAsync(modelName, prompt, deepThink ? 500 : 300);

            if (App.Config.IsModuleEnabled("web-search"))
            {
                answer += searchUsed ? "\n\n[used live web search]" : "\n\n[no web search result used - answered from own knowledge]";
            }

            return answer;
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
