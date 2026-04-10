using System.Threading;
using System.Threading.Tasks;

using Common;
using Infrastructure;

using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Workflows;

namespace Application.WorkflowOrchestrationLogic;

public static class Logic
{
    public sealed record ProcessingSettings(IChatClient FullLlm, IChatClient LightLlm, Neo4jService Neo4jService, CancellationToken cancellationToken = default);

    public static async Task ProcessEachUserQuestion(ProcessingSettings settings,
        Callbacks.ReportProgress fnReportProgress, params string[] userQuestionsToProcess)
    {
        foreach (var userQuestion in userQuestionsToProcess)
        {
            await ProcessUserQuestion(
                usingAgentsWorkflow: CreateAgentsWorkflow(settings),
                userQuestion: userQuestion, fnReportProgress, settings.cancellationToken);

            if(settings.cancellationToken.IsCancellationRequested)
                break;
        }
    }

    private static async Task ProcessUserQuestion(Workflow usingAgentsWorkflow, string userQuestion,
        Callbacks.ReportProgress fnReportProgress, CancellationToken cancellationToken)
    {
        await fnReportProgress($"User question: {userQuestion}");

        var workflowRun = await InProcessExecution.RunStreamingAsync(workflow: usingAgentsWorkflow, input: new Models.UserQuestion(Question: userQuestion), cancellationToken: cancellationToken);
        await workflowRun.TrySendMessageAsync(message: new TurnToken(emitEvents: true));

        await foreach (var evt in workflowRun.WatchStreamAsync(cancellationToken: cancellationToken))
        {
            if (evt is not WorkflowOutputEvent outputEvent) continue;

            await fnReportProgress(GetMessageToReport(fromEvent: outputEvent));
        }
    }

    private static Workflow CreateAgentsWorkflow(ProcessingSettings settings)
    {
        var cypherQueryBuilderExecutor = new CypherQueryBuilderExecutor(executorId: "CypherQueryBuilderExecutor",
            fromAgent: Utilities.CreateAgentThatBuildsCypherQueries(usingLlm: settings.FullLlm));
            
        var cypherQueryRunnerExecutor = new CypherQueryRunnerExecutor(settings.Neo4jService, executorId: "CypherQueryRunnerExecutor");

        var userResponseExecutor = new UserResponseExecutor(executorId: "UserResponseExecutor",
            fromAgent: Utilities.CreateAgentThatBuildsResponseToUser(usingLlm: settings.LightLlm));

        return new WorkflowBuilder(start: cypherQueryBuilderExecutor)
            .AddEdge(source: cypherQueryBuilderExecutor, target: cypherQueryRunnerExecutor)
            .AddEdge(source: cypherQueryRunnerExecutor, target: userResponseExecutor)
            .Build();
    }

    private static string GetMessageToReport(WorkflowOutputEvent fromEvent) => fromEvent.Data switch {
        Models.AttemptFailed attemptFailed => $"An attempt failed with message: {attemptFailed.FailureMessage}",
        Models.SuggestedCypherQuery suggested => $"Cypher Query to run: {suggested.Query}",
        Models.WorkflowReportingProgress progress => progress.Progress,
        Models.FinalResponseToUserQuestion finalResponse => $"Final answer: {finalResponse.Response}",
        _ => $"Unknown Workflow Output Event data type {fromEvent.Data.GetType()}"
    };

}
