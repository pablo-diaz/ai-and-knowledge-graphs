using System;
using System.Threading.Tasks;

using Common;
using Application;
using Infrastructure;

namespace ConsoleApp;

internal class Program
{
    static async Task Main(string[] args)
    {
        using ILargeLanguageModel llm = new GeminiService(
            apiKey: UserInputViaKeyboard.GetApiKey(
                fromProvider: Constants.LlmProvider.Gemini));

        //using ILargeLanguageModel llm = new OpenAiService(apiKey: GetApiKey(fromProvider: Constants.LlmProvider.OpenAI, args));

        using var neo4jService = new Neo4jService(url: "neo4j://ubuntu-server-01.mshome.net:7687");

        await OrchestrationLogic.GetAnswers(
            toUserQuestions: UserQuestionRetriever.GetQuestionsFromUser(
                maybeUserQuestionProvided: args.Length == 1 ? args[0] : null,
                fnGetQuestionFromUserInput: withPromptingUserMessage => UserInputViaKeyboard.PromptForUserInput(
                    withPromptingUserMessage: withPromptingUserMessage,
                    shouldHideWhatUserTypesIn: false)
            ),
            withLlmService: llm,
            withNeo4jService: neo4jService,
            fnReportProgress: progress => {
                Console.WriteLine(progress);
                return Task.CompletedTask;
            });
    }

}
