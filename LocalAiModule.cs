using System;
using System.IO;
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

            if (_loadedModelName == modelName && _executor != null)
                return true;

            var path = ModelFilePath(modelName);
            if (path == null)
            {
                error = $"({modelName} model file not found in models/{modelName}/ - make sure it's copied there)";
                return false;
            }

            // Unload whatever was loaded before to free RAM for the new model.
            _context?.Dispose();
            _model?.Dispose();
            _executor = null;

            var parameters = new ModelParams(path) { ContextSize = 1024 };
            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);
            _executor = new InteractiveExecutor(_context);
            _loadedModelName = modelName;
            return true;
        }

        public async Task<string> AskAsync(string userMessage, string modelName = "Ember")
        {
            if (!EnsureLoaded(modelName, out var error))
                return error;

            var systemPrompt = $"You are {modelName}, a helpful assistant. Always reply in English, in a friendly, concise way. Never repeat words or phrases. Never claim you searched the web or found something online unless real search results are given to you below.";

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
                    systemPrompt += " No search result was found for this question. Say so honestly instead of making something up, then answer from your own knowledge if you can, being clear it's not from a live search.";
                }
            }

            var prompt = $"<|im_start|>system\n{systemPrompt}<|im_end|>\n<|im_start|>user\n{userMessage}<|im_end|>\n<|im_start|>assistant\n";

            var inferenceParams = new InferenceParams
            {
                MaxTokens = 300,
                RepeatPenalty = 1.3f,
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
