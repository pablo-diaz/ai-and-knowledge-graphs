using System;
using System.Threading;
using System.Threading.Tasks;

using Infrastructure;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace Application.WorkflowOrchestrationLogic;

internal class CypherQueryBuilderExecutor : Executor<Models.UserQuestion, Models.CypherQueryToExecute>
{
    public readonly AIAgent _agent;
    private readonly string _executorId;
    private readonly CancellationToken _cancellationToken;

    public CypherQueryBuilderExecutor(AIAgent fromAgent, string executorId, CancellationToken cancellationToken) : base(id: executorId)
    {
        _agent = fromAgent;
        this._executorId = executorId;
        this._cancellationToken = cancellationToken;
    }

    public override async ValueTask<Models.CypherQueryToExecute> HandleAsync(Models.UserQuestion message, IWorkflowContext context, CancellationToken ct)
    {
        await Utilities.PublishWorkflowProgressEvent(progressMessageToReport: "Creating cypher query ...", context, _executorId, _cancellationToken);

        var responseFromAgent = await AgentUtilities.ResilientlyCallAgent(
            settings: new AgentUtilities.AgentCallingSettings(
                fnCallAgentAsync: ct => CallAgent(withUserQuestion: message),
                MaxRetries: 10,
                EveryMilliseconds: 2_000,
                OnEachRetry: (e, t, r) => HandleRetryError(context, e, t, r)
                ),
            cancellationToken: _cancellationToken
            );

        await Utilities.PublishWorkflowProgressEvent(progressMessageToReport: "Cypher query was created successfully ...", context, _executorId, _cancellationToken);

        var suggestedQuery = System.Text.Json.JsonSerializer.Deserialize<Models.SuggestedCypherQuery>(responseFromAgent);

        await context.AddEventAsync(workflowEvent: new WorkflowOutputEvent(data: suggestedQuery, executorId: _executorId), cancellationToken: _cancellationToken);

        return new Models.CypherQueryToExecute(CypherQuery: suggestedQuery, WithUserQuestion: message);
    }

    private async ValueTask<string> CallAgent(Models.UserQuestion withUserQuestion)
    {
        if (_cancellationToken.IsCancellationRequested) return string.Empty;

        return (await _agent.RunAsync(message: withUserQuestion.Question, cancellationToken: _cancellationToken))
            .Text;
    }
    
    private async ValueTask HandleRetryError(IWorkflowContext context, Exception ex, TimeSpan ts, int r)
    {
        await context.AddEventAsync(
            workflowEvent: new WorkflowOutputEvent(
                data: new Models.AttemptFailed(FailureMessage: $"Whilst calling the Agent to create the Cypher Query, it failed with reason '{ex.Message}'. It is the {r}th attempt and this request will be retried after {ts.TotalMilliseconds} ms"),
                executorId: _executorId),
            cancellationToken: _cancellationToken);
    }

}
