using System;
using System.Linq;
using System.Threading.Tasks;

using Neo4j.Driver;

namespace Infrastructure;

public class Neo4jService : IDisposable
{
    public sealed record Row(string[] Values);
    public abstract record QueryResult;
    public sealed record QueryDidNotReturnAnyContent : QueryResult;
    public sealed record QueryCouldNotBeExecuted(string Reason) : QueryResult;

    public sealed record QueryResultWithContent(string[] Headers, Row[] Rows) : QueryResult
    {
        public string ToMarkdownTable()
        {
            var headerRow = $"| {string.Join(separator: " | ", Headers)} |";
            var separatorRow = $"|{string.Join(separator: "|", Enumerable.Repeat("---", Headers.Length))}|";
            var dataRows = string.Join(Environment.NewLine, Rows.Select(row => $"| {string.Join(separator: " | ", row.Values)} |"));
            return $"{headerRow}{Environment.NewLine}{separatorRow}{Environment.NewLine}{dataRows}";
        }
    }

    private readonly IDriver _client;
    
    public Neo4jService(string url)
    {
        _client = GraphDatabase.Driver(new Uri(uriString: url));
    }

    public async Task<QueryResult> ExecuteQuery(string cypherQuery, int maxRowsToReturn)
    {
        try
        {
            var queryResults = await _client.ExecutableQuery(cypher: cypherQuery).ExecuteAsync();

            return false == queryResults.Result.Any() 
                ? new QueryDidNotReturnAnyContent()
                : new QueryResultWithContent(
                    Headers: queryResults.Keys,
                    Rows: [.. queryResults.Result
                            .Select(record => new Row(
                                Values: [.. queryResults.Keys.Select(record.Get<string>)]))
                            .Take(count: maxRowsToReturn)
                        ]
                    );
        }
        catch (Exception ex)
        {
            return new QueryCouldNotBeExecuted(Reason: ex.Message);
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
