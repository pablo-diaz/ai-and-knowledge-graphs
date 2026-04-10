using Application.WorkflowOrchestrationLogic;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebAppForWorkflows.Services;

public class WorkflowSessionsHub
{
    public sealed record Session(string Id)
    {
        public static Session New() => new(Id: Guid.NewGuid().ToString());
        public static Session From(string id) => new(Id: id);
    }

    private sealed record QuestionAsked(string Question);

    private readonly Dictionary<Session, QuestionAsked> _questionPerSession = new();
    private readonly Neo4jService _neo4jService;
    private readonly DecoratorForChatClient _fullLlm;
    private readonly DecoratorForChatClient _lightLlm;

    public WorkflowSessionsHub(Neo4jService neo4jService, [FromKeyedServices("FullLlm")] DecoratorForChatClient fullLlm,
        [FromKeyedServices("LightLlm")] DecoratorForChatClient lightLlm)
    {
        _neo4jService = neo4jService;
        this._fullLlm = fullLlm;
        this._lightLlm = lightLlm;
    }

    public Session Ask(string questionToAsk)
    {
        var newSession = Session.New();
        _questionPerSession[newSession] = new QuestionAsked(questionToAsk);
        return newSession;
    }

    public async Task GetStreamOfAnswersAsync(Stream intoStream, Session ofSession, CancellationToken ct)
    {
        var writer = new StreamWriter(intoStream);

        if (false == _questionPerSession.ContainsKey(ofSession))
        {
            await WriteProgress(toStream: writer, messageType: "finished", messageToWrite: "now");
            return;
        }

        /*
        foreach(var answer in GetCannedAnsersForTestingPurposes())
        {
            await WriteProgress(writer, "progressReport", answer);
            await Task.Delay(2_000, ct);
        }
        */

        await Logic.ProcessEachUserQuestion(
            userQuestionsToProcess: _questionPerSession[ofSession].Question,
            settings: new(FullLlm: _fullLlm, LightLlm: _lightLlm, _neo4jService, cancellationToken: ct),
            fnReportProgress: progressMessage => WriteProgress(toStream: writer, messageType: "progressReport", messageToWrite: progressMessage));

        await WriteProgress(toStream: writer, messageType: "finished", messageToWrite: "now");
    }

    private static async Task WriteProgress(StreamWriter toStream, string messageType, string messageToWrite)
    {
        var payload = new { content = messageToWrite };

        string jsonPayload = JsonSerializer.Serialize(payload);

        await toStream.WriteAsync($"event: {messageType}\n");
        await toStream.WriteAsync($"data: {jsonPayload}\n\n");
        await toStream.FlushAsync();

        Console.WriteLine(messageToWrite);
    }

    private static IEnumerable<string> GetCannedAnsersForTestingPurposes()
    {
        string[] answers = [
            """
            # Cypher Query Example

            Here is a **Cypher query** for Neo4j:
            """,

            """
            MATCH (u:User)-[:FOLLOWS]->(other:User) WHERE u.name = "Alice" RETURN other.name, count(*) AS followers ORDER BY followers DESC LIMIT 5;
            """,

            """
            This is just a **normal** paragraph with *italic* and **bold** text.

            - Bullet list item 1
                - sub item 1
                - sub item 3
                - sub item 2
            - Bullet list item 2
            """,

            """
            # Table example

            | Column | Description |
            |--------|-------------|
            | A      | Alpha       |
            | B      | Beta        |
            """,

            """
            MATCH (u:User)-[:FOLLOWS]->(other:User) WHERE u.name = "Alice" RETURN other.name, count(*) AS followers ORDER BY followers DESC LIMIT 5;
            """,

            """
            # Another header
            ## Sub header
            ### Sub sub header
            #### Sub sub sub header
            """,

            """
            MATCH (u:User)-[:FOLLOWS]->(other:User) WHERE u.name = "Alice" RETURN other.name, count(*) AS followers ORDER BY followers DESC LIMIT 5;
            """,

            """
            # Table example

            | Column | Description |
            |--------|-------------|
            | A      | Alpha       |
            | B      | Beta        |
            """
        ];

        foreach (var answer in answers)
            yield return answer;
    }

}
