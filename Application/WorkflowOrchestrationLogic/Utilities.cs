using System.Threading;
using System.Threading.Tasks;

using Common;
using Infrastructure;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Workflows;

namespace Application.WorkflowOrchestrationLogic;

public static class Utilities
{
    public static string[] GetQuestionsFromUser(string maybeUserQuestionProvided) =>
        UserQuestionRetriever.GetQuestionsFromUser(
            maybeUserQuestionProvided: maybeUserQuestionProvided,
            fnGetQuestionFromUserInput: withPromptingUserMessage => UserInputViaKeyboard.PromptForUserInput(
                withPromptingUserMessage: withPromptingUserMessage,
                shouldHideWhatUserTypesIn: false)
        );

    internal static AIAgent CreateAgentThatBuildsCypherQueries(IChatClient usingLlm) =>
        new ChatClientAgent(chatClient: usingLlm,
            options: new ChatClientAgentOptions
            {
                Name = "CypherQueryBuilderAgent",
                ChatOptions = new()
                {
                    ResponseFormat = ChatResponseFormat.ForJsonSchema<Models.SuggestedCypherQuery>(),
                    Instructions = GraphService.GetSystemPromptToGenerateCypherQueriesFromGraphSchema()
                }
            }
        );

    internal static AIAgent CreateAgentThatBuildsResponseToUser(IChatClient usingLlm) =>
        new ChatClientAgent(chatClient: usingLlm, name: "UserResponseAgent",
            instructions: """
            You are a helpful assistant that prepares a full response to a user question, based on data that you receive as input.
            """
        );

    internal static async Task PublishWorkflowProgressEvent(string progressMessageToReport,
        IWorkflowContext context, string executorId, CancellationToken cancellationToken)
    {
        await context.AddEventAsync(
            workflowEvent: new WorkflowOutputEvent(
                data: new Models.WorkflowReportingProgress(Progress: progressMessageToReport),
                executorId: executorId),
            cancellationToken: cancellationToken);
    }

}
