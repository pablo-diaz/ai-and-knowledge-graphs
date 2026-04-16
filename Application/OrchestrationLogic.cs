using System;
using System.Threading.Tasks;

using Common;
using Infrastructure;

namespace Application;

public class OrchestrationLogic
{
    private delegate Task<string> EnhanceAnswerWithContent(Neo4jService.QueryResultWithContent dbContent);

    public static async Task GetAnswers(ILargeLanguageModel withLlmService, Neo4jService withNeo4jService,
        Callbacks.ReportProgress fnReportProgress, params string[] toUserQuestions)
    {
        foreach (var userQuestion in toUserQuestions)
        {
            await GetAnswer(
                toUserQuestion: userQuestion,
                withLlmService: withLlmService,
                withNeo4jService: withNeo4jService,
                fnReportProgress);
        }
    }

    private static async Task GetAnswer(string toUserQuestion, ILargeLanguageModel withLlmService,
        Neo4jService withNeo4jService, Callbacks.ReportProgress fnReportProgress)
    {
        await fnReportProgress(new Callbacks.Information($"User question: {toUserQuestion}"));

        var cypherQueryToRun = await GetCypherQueryToUse(toUserQuestion: toUserQuestion, withLlmService: withLlmService);
        await fnReportProgress(new Callbacks.CypherQueryToExecute(cypherQueryToRun));

        var completeAnswer = await CheckQueryResult(
            queryResult: await withNeo4jService.ExecuteQueryAsync(cypherQuery: cypherQueryToRun, maxRowsToReturn: 50, cancellationToken: default),
            fnEnhanceAnswerWithContent: async (content) => await EnhanceAnswer(toUserQuestion: toUserQuestion, usingNeo4jResults: content, withLlmService: withLlmService));

        await fnReportProgress(new Callbacks.FinalUserResponse(completeAnswer));
    }

    private static async Task<string> GetCypherQueryToUse(string toUserQuestion, ILargeLanguageModel withLlmService) =>
        (await withLlmService.GetStructuredResponse<GraphService.SuggestedQuery>(
            withUserMessage: GraphService.BuildUserMessageToGetCypherQuery(fromUserQuestion: toUserQuestion),
            withSystemPrompt: GraphService.GetSystemPromptToGenerateCypherQueriesFromGraphSchema(),
            usingJsonStructuredOutputSchema: GraphService.CreateJsonStructuredOutputSchemaForSuggestedQueryDTO())
        )
        .Query;

    private static async Task<string> CheckQueryResult(Neo4jService.QueryResult queryResult,
            EnhanceAnswerWithContent fnEnhanceAnswerWithContent) => queryResult switch {
        Neo4jService.QueryDidNotReturnAnyContent _ => "The query ran successfully, but it did not return any content.",
        Neo4jService.QueryCouldNotBeExecuted q => $"The query could not be executed. Reason: {q.Reason}",
        Neo4jService.QueryResultWithContent content => await fnEnhanceAnswerWithContent(content),
        _ => throw new InvalidOperationException($"Unknown query result type '{queryResult.GetType()}'")
    };

    private static async Task<string> EnhanceAnswer(string toUserQuestion, Neo4jService.QueryResultWithContent usingNeo4jResults, ILargeLanguageModel withLlmService) =>
        await withLlmService.GetBasicResponse(
            withUserMessage: GraphService.BuildUserMessageToEnhanceAnswer(
                toUserQuestion: toUserQuestion,
                usingTableInformation: usingNeo4jResults.ToMarkdownTable()));

}
