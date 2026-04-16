using System.Threading;
using System.Threading.Tasks;

using Infrastructure;

using Microsoft.Agents.AI.Workflows;

namespace Application.WorkflowOrchestrationLogic;

internal class CypherQueryRunnerExecutor : Executor<Models.CypherQueryToExecute, Models.ResponseToAugment>
{
    private readonly Neo4jService _neo4JService;
    private readonly string _executorId;
    private readonly CancellationToken _cancellationToken;

    public CypherQueryRunnerExecutor(Neo4jService neo4JService, string executorId, CancellationToken cancellationToken) : base(id: executorId)
    {
        this._neo4JService = neo4JService;
        this._executorId = executorId;
        this._cancellationToken = cancellationToken;
    }

    public override async ValueTask<Models.ResponseToAugment> HandleAsync(Models.CypherQueryToExecute message, IWorkflowContext context, CancellationToken ct)
    {
        await Utilities.PublishWorkflowProgressEvent(progressMessageToReport: "Running cypher query in Neo4j ...", context, _executorId, _cancellationToken);
        var results = await _neo4JService.ExecuteQueryAsync(cypherQuery: message.CypherQuery.Query, maxRowsToReturn: 100, cancellationToken: _cancellationToken);
        await Utilities.PublishWorkflowProgressEvent(progressMessageToReport: $"Finished running cypher query in Neo4j. Result is of type '{results.GetType().FullName}'", context, _executorId, _cancellationToken);
        
        return new(GraphData: results, WithUserQuestion: message.WithUserQuestion);
    }

}
