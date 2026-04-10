using System.Threading;
using System.Threading.Tasks;

using Infrastructure;

using Microsoft.Agents.AI.Workflows;

namespace Application.WorkflowOrchestrationLogic;

internal class CypherQueryRunnerExecutor : Executor<Models.CypherQueryToExecute, Models.ResponseToAugment>
{
    private readonly Neo4jService _neo4JService;
    private readonly string _executorId;

    public CypherQueryRunnerExecutor(Neo4jService neo4JService, string executorId) : base(id: executorId)
    {
        this._neo4JService = neo4JService;
        this._executorId = executorId;
    }

    public override async ValueTask<Models.ResponseToAugment> HandleAsync(Models.CypherQueryToExecute message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        await Utilities.PublishWorkflowProgressEvent(progressMessageToReport: "Running cypher query in Neo4j ...", context, _executorId, cancellationToken);
        var results = await _neo4JService.ExecuteQuery(cypherQuery: message.CypherQuery.Query, maxRowsToReturn: 100);
        await Utilities.PublishWorkflowProgressEvent(progressMessageToReport: $"Finished running cypher query in Neo4j. Result is of type '{results.GetType().FullName}'", context, _executorId, cancellationToken);
        
        return new(GraphData: results, WithUserQuestion: message.WithUserQuestion);
    }

}
