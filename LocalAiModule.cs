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
        private bool _isLoaded;

        private static string ModelPath =>
            Path.Combine(AppContext.BaseDirectory, "models", "Ember", "qwen2.5-0.5b-instruct-q4_k_m.gguf");

        public bool Load()
        {
            if (_isLoaded) return true;
            if (!File.Exists(ModelPath)) return false;

            var parameters = new ModelParams(ModelPath) { ContextSize = 1024 };
            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);
            _executor = new InteractiveExecutor(_context);
            _isLoaded = true;
            return true;
        }

        public async Task<string> AskAsync(string userMessage)
        {
            if (!_isLoaded && !Load())
                return "(Ember model file not found in models/Ember/ - make sure it's copied there)";

            var systemPrompt = "You are Ember, a helpful assistant. Always reply in English, in a friendly, concise way. Never repeat words or phrases.";

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
                    systemPrompt += " Here is some extra information that may help, use it only if relevant: " + searchResult;
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
