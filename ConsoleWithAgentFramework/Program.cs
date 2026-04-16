using System;
using System.Threading.Tasks;

using Common;
using Infrastructure;
using Application.WorkflowOrchestrationLogic;

namespace ConsoleWithAgentFramework;

internal class Program
{
    static async Task Main(string[] args)
    {
        var geminiApiKey = UserInputViaKeyboard.GetApiKey(fromProvider: Constants.LlmProvider.Gemini);

        using var neo4jService = new Neo4jService(url: "neo4j://ubuntu-server-01.mshome.net:7687");

        using var fullLlm = new DecoratorForChatClient(provider: Constants.LlmProvider.Gemini,
            llmApiKey: geminiApiKey, modelName: Constants.LlmModels.Gemini.FullModel);

        using var lightLlm = new DecoratorForChatClient(provider: Constants.LlmProvider.Gemini,
            llmApiKey: geminiApiKey, modelName: Constants.LlmModels.Gemini.LightModel);

        await Logic.ProcessEachUserQuestion(
            userQuestionsToProcess: Utilities.GetQuestionsFromUser(
                maybeUserQuestionProvided: args.Length == 1 ? args[0] : null),
            settings: new(FullLlm: fullLlm, LightLlm: lightLlm, neo4jService, cancellationToken: default),
            fnReportProgress: progress => {
                Console.WriteLine(progress);
                return Task.CompletedTask;
            });
    }

}
