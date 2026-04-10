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

    public CypherQueryBuilderExecutor(AIAgent fromAgent, string executorId) : base(id: executorId)
    {
        _agent = fromAgent;
        this._executorId = executorId;
    }

    public override async ValueTask<Models.CypherQueryToExecute> HandleAsync(Models.UserQuestion message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        await Utilities.PublishWorkflowProgressEvent(progressMessageToReport: "Creating cypher query ...", context, _executorId, cancellationToken);

        var responseFromAgent = await AgentUtilities.ResilientlyCallGeminiAgent(
            settings: new AgentUtilities.GeminiAgentCallSettings(
                fnCallAgent: () => CallAgent(withUserQuestion: message, cancellationToken),
                MaxRetries: 10,
                EveryMilliseconds: 2_000,
                OnEachRetry: (e, t, r) => HandleRetryError(context, e, t, r, cancellationToken)
                )
            );

        await Utilities.PublishWorkflowProgressEvent(progressMessageToReport: "Cypher query was created successfully ...", context, _executorId, cancellationToken);

        var suggestedQuery = System.Text.Json.JsonSerializer.Deserialize<Models.SuggestedCypherQuery>(responseFromAgent);

        await context.AddEventAsync(workflowEvent: new WorkflowOutputEvent(data: suggestedQuery, executorId: _executorId), cancellationToken: cancellationToken);

        return new Models.CypherQueryToExecute(CypherQuery: suggestedQuery, WithUserQuestion: message);
    }

    private async ValueTask<string> CallAgent(Models.UserQuestion withUserQuestion, CancellationToken cancellationToken) =>
        (await _agent.RunAsync(message: withUserQuestion.Question, cancellationToken: cancellationToken))
        .Text;
    
    private async ValueTask HandleRetryError(IWorkflowContext context, Exception ex, TimeSpan ts, int r, CancellationToken cancellationToken)
    {
        await context.AddEventAsync(
            workflowEvent: new WorkflowOutputEvent(
                data: new Models.AttemptFailed(FailureMessage: $"Whilst calling the Agent to create the Cypher Query, it failed with reason '{ex.Message}'. It is the {r}th attempt and this request will be retried after {ts.TotalMilliseconds} ms"),
                executorId: _executorId),
            cancellationToken: cancellationToken);
    }

}
