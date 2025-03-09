using Backend.Integrations.Interfaces;

namespace Backend.Integrations;

/// <summary>
/// Простой генератор текста, создающий предложения на основе предоставленных слов.
/// </summary>
public class SimpleTextGenerator : ITextGenerator
{
    private readonly Random _random = new();

    /// <summary>
    /// Генерирует текст на основе списка слов.
    /// </summary>
    public async Task<string> GenerateTextAsync(IEnumerable<string> words)
    {
        var wordList = words.ToList();
        if (!wordList.Any())
            return "No words provided for text generation.";

        var sentences = new List<string>();
        var wordsPerSentence = Math.Min(5, wordList.Count);
        
        for (var i = 0; i < wordList.Count; i += wordsPerSentence)
        {
            var sentenceWords = wordList
                .Skip(i)
                .Take(wordsPerSentence)
                .ToList();
            
            if (sentenceWords.Any())
            {
                var sentence = $"Here are some words to learn: {string.Join(", ", sentenceWords)}.";
                sentences.Add(sentence);
            }
        }

        return await Task.FromResult(string.Join("\n", sentences));
    }

    /// <summary>
    /// Генерирует текст на основе слов с их переводами.
    /// </summary>
    public async Task<string> GenerateTextWithTranslationsAsync(IDictionary<string, string> wordsWithTranslations)
    {
        if (!wordsWithTranslations.Any())
            return "No words provided for text generation.";

        var sentences = new List<string>();
        var words = wordsWithTranslations.ToList();
        var wordsPerSentence = Math.Min(3, words.Count);

        for (var i = 0; i < words.Count; i += wordsPerSentence)
        {
            var sentenceWords = words
                .Skip(i)
                .Take(wordsPerSentence)
                .ToList();

            if (sentenceWords.Any())
            {
                var wordPairs = sentenceWords
                    .Select(w => $"{w.Key} ({w.Value})")
                    .ToList();
                
                var sentence = $"Let's practice these words: {string.Join(", ", wordPairs)}.";
                sentences.Add(sentence);
            }
        }

        return await Task.FromResult(string.Join("\n", sentences));
    }
} 