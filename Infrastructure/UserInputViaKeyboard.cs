using System;
using System.Text;

using Common;

namespace Infrastructure;

public class UserInputViaKeyboard
{
    public static string GetApiKey(Constants.LlmProvider fromProvider)
    {
        var maybeGeminiApiKeySetAsEnvironmentVariable = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (fromProvider == Constants.LlmProvider.Gemini && !string.IsNullOrEmpty(maybeGeminiApiKeySetAsEnvironmentVariable))
            return maybeGeminiApiKeySetAsEnvironmentVariable;

        var maybeOpenAiApiKeySetAsEnvironmentVariable = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (fromProvider == Constants.LlmProvider.OpenAI && !string.IsNullOrEmpty(maybeOpenAiApiKeySetAsEnvironmentVariable))
            return maybeOpenAiApiKeySetAsEnvironmentVariable;

        return PromptForUserInput(
            withPromptingUserMessage: $"Please enter your API key for {fromProvider}",
            shouldHideWhatUserTypesIn: true);
    }

    public static string PromptForUserInput(string withPromptingUserMessage, bool shouldHideWhatUserTypesIn)
    {
        Console.Write($"{withPromptingUserMessage}: ");
        StringBuilder apiKeyTypingBuilder = new();
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(intercept: shouldHideWhatUserTypesIn);  // true hides the key pressed
            if (key.Key != ConsoleKey.Enter)
            {
                apiKeyTypingBuilder.Append(key.KeyChar);

                if (shouldHideWhatUserTypesIn)
                    Console.Write("*");
            }
        } while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();

        return apiKeyTypingBuilder.ToString();
    }

}
