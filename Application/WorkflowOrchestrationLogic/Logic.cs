using Common;
using Infrastructure;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.WorkflowOrchestrationLogic;

public static class Logic
{
    public sealed record ProcessingSettings(IChatClient FullLlm, IChatClient LightLlm, Neo4jService Neo4jService, CancellationToken cancellationToken);

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
        await fnReportProgress(new Callbacks.Information($"User question: {userQuestion}"));

        var workflowRun = await InProcessExecution.RunStreamingAsync(
            workflow: usingAgentsWorkflow, 
            input: new Models.UserQuestion(Question: userQuestion), 
            cancellationToken: cancellationToken);

        await workflowRun.TrySendMessageAsync(message: new TurnToken(emitEvents: true));

        await foreach (var evt in workflowRun
                                  .WatchStreamAsync(cancellationToken: cancellationToken)
                                  .WithCancellation(cancellationToken))
        {
            if (evt is not WorkflowOutputEvent outputEvent) continue;

            foreach(var messageToReport in GetMessagesToReport(fromEvent: outputEvent))
                await fnReportProgress(messageToReport);
        }
    }

    private static Workflow CreateAgentsWorkflow(ProcessingSettings settings)
    {
        var cypherQueryBuilderExecutor = new CypherQueryBuilderExecutor(
            executorId: "CypherQueryBuilderExecutor",
            fromAgent: Utilities.CreateAgentThatBuildsCypherQueries(usingLlm: settings.FullLlm),
            cancellationToken: settings.cancellationToken);
            
        var cypherQueryRunnerExecutor = new CypherQueryRunnerExecutor(
            neo4JService: settings.Neo4jService,
            executorId: "CypherQueryRunnerExecutor",
            cancellationToken: settings.cancellationToken);

        var userResponseExecutor = new UserResponseExecutor(
            executorId: "UserResponseExecutor",
            fromAgent: Utilities.CreateAgentThatBuildsResponseToUser(usingLlm: settings.LightLlm),
            cancellationToken: settings.cancellationToken);

        return new WorkflowBuilder(start: cypherQueryBuilderExecutor)
            .AddEdge(source: cypherQueryBuilderExecutor, target: cypherQueryRunnerExecutor)
            .AddEdge(source: cypherQueryRunnerExecutor, target: userResponseExecutor)
            .Build();
    }

    private static Callbacks.ProgressUpdate[] GetMessagesToReport(WorkflowOutputEvent fromEvent) => fromEvent.Data switch {
        Models.AttemptFailed attemptFailed => [
            new Callbacks.Error($"An attempt failed with message: {attemptFailed.FailureMessage}"),
            new Callbacks.Information("Underlying AI service transient/temporal error. Retrying ...")
        ],

        Models.SuggestedCypherQuery suggested => [new Callbacks.CypherQueryToExecute(suggested.Query)],
        
        Models.WorkflowReportingProgress progress => [new Callbacks.Information(progress.Progress)],
        
        Models.FinalResponseToUserQuestion finalResponse => [new Callbacks.FinalUserResponse(finalResponse.Response)],
        
        _ => [new Callbacks.Information($"Unknown Workflow Output Event data type {fromEvent.Data.GetType()}")]
    };

}
