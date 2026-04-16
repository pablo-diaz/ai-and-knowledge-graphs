using Common;
using Infrastructure;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.WorkflowOrchestrationLogic;

internal class UserResponseExecutor : Executor<Models.ResponseToAugment, Models.FinalResponseToUserQuestion>
{
    public readonly AIAgent _agent;
    private readonly string _executorId;
    private readonly CancellationToken _cancellationToken;

    public UserResponseExecutor(AIAgent fromAgent, string executorId, CancellationToken cancellationToken) : base(id: executorId)
    {
        _agent = fromAgent;
        this._executorId = executorId;
        this._cancellationToken = cancellationToken;
    }

    public override async ValueTask<Models.FinalResponseToUserQuestion> HandleAsync(Models.ResponseToAugment message, IWorkflowContext context, CancellationToken ct)
    {
        await Utilities.PublishWorkflowProgressEvent(progressMessageToReport: "Creating final answer to user ...", context, _executorId, _cancellationToken);
        var responseFromAgent = await GetFinalAnswerToUser(fromResponse: message, context);
        await Utilities.PublishWorkflowProgressEvent(progressMessageToReport: "Final answer to user was created successfully", context, _executorId, _cancellationToken);

        var finalResponse = new Models.FinalResponseToUserQuestion(Response: responseFromAgent);
        await context.AddEventAsync(workflowEvent: new WorkflowOutputEvent(data: finalResponse, executorId: _executorId), cancellationToken: _cancellationToken);

        return finalResponse;
    }

    private async Task<string> GetFinalAnswerToUser(Models.ResponseToAugment fromResponse, IWorkflowContext context)
    {
        return fromResponse.GraphData switch {
            Neo4jService.QueryDidNotReturnAnyContent _ => "No results were found in graph database",
            Neo4jService.QueryCouldNotBeExecuted e => $"Suggested cypher query could not be executed. Reason: {e.Reason}",
            Neo4jService.QueryResultWithContent graphData => await AgentUtilities.ResilientlyCallAgent(
                settings: new AgentUtilities.AgentCallingSettings(
                    fnCallAgentAsync: async ct => await CallAgent(userQuestion: fromResponse.WithUserQuestion, graphData: graphData),
                    MaxRetries: 10,
                    EveryMilliseconds: 2_000,
                    OnEachRetry: (e, t, r) => HandleRetryError(context, e, t, r)
                ),
                cancellationToken: _cancellationToken
            ),
            _ => throw new InvalidOperationException($"The type of GraphData is not supported: {fromResponse.GraphData.GetType().FullName}")
        };
    }

    private async ValueTask<string> CallAgent(Models.UserQuestion userQuestion, Neo4jService.QueryResultWithContent graphData)
    {
        if (_cancellationToken.IsCancellationRequested) return string.Empty;

        return (await _agent.RunAsync(
            message: GraphService.BuildUserMessageToEnhanceAnswer(
                toUserQuestion: userQuestion.Question,
                usingTableInformation: graphData.ToMarkdownTable()),
            cancellationToken: _cancellationToken))
        .Text;
    }
    
    private async ValueTask HandleRetryError(IWorkflowContext context, Exception ex, TimeSpan ts, int r)
    {
        await context.AddEventAsync(
            workflowEvent: new WorkflowOutputEvent(
                data: new Models.AttemptFailed(FailureMessage: $"Whilst calling the Agent to create the final response to the user, it failed with reason '{ex.Message}'. It is the {r}th attempt and this request will be retried after {ts.TotalMilliseconds} ms"),
                executorId: _executorId),
            cancellationToken: _cancellationToken);
    }

}
