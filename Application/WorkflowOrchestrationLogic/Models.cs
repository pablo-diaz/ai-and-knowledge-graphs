using Infrastructure;

namespace Application.WorkflowOrchestrationLogic;

internal class Models
{
    public sealed record UserQuestion(string Question);
    public sealed record SuggestedCypherQuery([property: System.Text.Json.Serialization.JsonPropertyName("query")] string Query);
    public sealed record CypherQueryToExecute(SuggestedCypherQuery CypherQuery, UserQuestion WithUserQuestion);
    public sealed record FinalResponseToUserQuestion(string Response);
    public sealed record ResponseToAugment(Neo4jService.QueryResult GraphData, UserQuestion WithUserQuestion);
    public sealed record WorkflowReportingProgress(string Progress);
    public sealed record AttemptFailed(string FailureMessage);
}
