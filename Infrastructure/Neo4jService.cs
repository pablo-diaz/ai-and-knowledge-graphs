using Neo4j.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    public async Task<QueryResult> ExecuteQueryAsync(string cypherQuery, int maxRowsToReturn, CancellationToken cancellationToken)
    {
        try
        {
            var queryResults = await _client
                .ExecutableQuery(cypher: cypherQuery)
                .ExecuteAsync(token: cancellationToken);

            return false == queryResults.Result.Any() 
                ? new QueryDidNotReturnAnyContent()
                : new QueryResultWithContent(
                    Headers: queryResults.Keys,
                    Rows: [.. queryResults.Result
                            .Select(record => new Row(
                                Values: [.. queryResults.Keys.Select((keyName, pos) => GetStringRepresentation(ofObject: record[pos]))]))
                            .Take(count: maxRowsToReturn)
                        ]
                    );
        }
        catch (Exception ex)
        {
            return new QueryCouldNotBeExecuted(Reason: ex.Message);
        }
    }

    private static string GetStringRepresentation(object ofObject)
    {
        var theType = ofObject.GetType();

        var r = ofObject switch {
            IEnumerable<object> list => GetStringRepresentationOfList(list),
            Dictionary<string, object> dict => GetStringRepresentationOfDictionary(dict),
            _ => ofObject.ToString() ?? string.Empty
        };

        return r;
    }

    private static string GetStringRepresentationOfList(IEnumerable<object> list) =>
        $"[{string.Join(separator: ", ", list.Select(GetStringRepresentation))}]";

    private static string GetStringRepresentationOfDictionary(Dictionary<string, object> dict) =>
        $"{{{string.Join(separator: ", ", dict.Select(kv => $"{GetStringRepresentation(kv.Key)}: {GetStringRepresentation(kv.Value)}"))}}}";

    public void Dispose()
    {
        _client?.Dispose();
    }
}
