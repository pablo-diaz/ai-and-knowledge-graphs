using Application.WorkflowOrchestrationLogic;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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

    public async Task PutResponsesAsync(Stream intoStream, Session ofSession, CancellationToken ct)
    {
        var writer = new StreamWriter(intoStream);

        if (false == _questionPerSession.ContainsKey(ofSession))
        {
            await FinishStream(writer);
            return;
        }

        //await WriteMessage(writer, "userResponse", await GetContentOfComplexTable(ct));

        /*
        await foreach (var answer in GetCannedAnsersForTestingPurposesAsync(ct))
            await WriteMessage(writer, "userResponse", answer);
        */

        try
        {
            await Logic.ProcessEachUserQuestion(
                userQuestionsToProcess: _questionPerSession[ofSession].Question,
                settings: new(FullLlm: _fullLlm, LightLlm: _lightLlm, _neo4jService, cancellationToken: ct),
                fnReportProgress: progressMessage => WriteMessage(toStream: writer, messageToWrite: progressMessage));
        }
        catch (Exception ex)
        {
            await WriteMessage(writer, "errorsReport", ex.Message);
        }

        await FinishStream(writer);
    }

    private static async Task FinishStream(StreamWriter streamToFinish)
    {
        await WriteMessage(toStream: streamToFinish, messageType: "finished", messageToWrite: "now");
    }

    private static async Task WriteMessage(StreamWriter toStream, Common.Callbacks.ProgressUpdate messageToWrite)
    {
        switch(messageToWrite)
        {
            case Common.Callbacks.Information info:
                await WriteMessage(toStream, "progressReport", info.Description);
                break;
            case Common.Callbacks.Error info:
                await WriteMessage(toStream, "warningsReport", info.Description);
                break;
            case Common.Callbacks.CypherQueryToExecute query:
                await WriteMessage(toStream, "queryCreated", query.Query);
                break;
            case Common.Callbacks.FinalUserResponse response:
                await WriteMessage(toStream, "userResponse", response.Content);
                break;
            default:
                await WriteMessage(toStream, "errorsReport", $"Unknown message type '{messageToWrite.GetType()}'");
                break;
        }
    }

    private static async Task WriteMessage(StreamWriter toStream, string messageType, string messageToWrite)
    {
        var payload = new { content = messageToWrite };

        string jsonPayload = JsonSerializer.Serialize(payload);

        await toStream.WriteAsync($"event: {messageType}\n");
        await toStream.WriteAsync($"data: {jsonPayload}\n\n");
        await toStream.FlushAsync();

        Console.WriteLine(messageToWrite);
    }

    private async Task<string> GetContentOfComplexTable(CancellationToken cancellationToken)
    {
        var r = await _neo4jService.ExecuteQueryAsync(cypherQuery: """
            UNWIND ["37", "38", "72", "49", "78", "28", "20", "36", "99", "61", "01", "04", "21", "63"] AS labId
            MATCH (l:Laboratory {id: labId})<-[:logged_in_at]-(o:Order)-[:sold_by]->(e:Employee)
            WITH l, e, COUNT(o) AS salesCount 
            ORDER BY salesCount DESC 
            WITH l, COLLECT({salesRep: e.id, count: salesCount})[0..10] AS topReps 
            RETURN l.id AS laboratoryId, l.name AS laboratoryName, topReps
            """, maxRowsToReturn: 100, cancellationToken);

        return ((Neo4jService.QueryResultWithContent)r).ToMarkdownTable();
    }

    private static async IAsyncEnumerable<string> GetCannedAnsersForTestingPurposesAsync([EnumeratorCancellation] CancellationToken ct)
    {
        string[] answers = [
            """
            Based on the information provided, here are the pie chart and the bar chart illustrating the top 10 customers that have submitted the most orders.

            ***

            ### 📊 Top 10 Customers by Orders Submitted (Bar Chart)

            This bar chart visually compares the total orders submitted by each of the top 10 customers.

            ```mermaid
            xychart-beta
                title "Top 10 Customers by Orders Submitted for Lab 55"
                x-axis "Customers" [AAA, BBB, CCC, DDD, EEE, FFF, GGG, HHH, III, JJJ]
                y-axis "Orders Submitted" 0 --> 14000
                bar [12350, 10250, 9430, 8200, 7600, 5200, 5100, 4200, 4100, 2544]
            ```

            ### 🥧 Top 10 Customers by Orders Submitted (Pie Chart)

            This pie chart illustrates the proportional contribution of orders from each of the top 10 customers relative to the total orders within this data set.

            ```mermaid
            pie
                title Top 10 Customers by Orders Submitted for Lab 55
                "AAA" : 12350
                "BBB" : 10250
                "CCC" : 9430
                "DDD" : 8200
                "EEE" : 7600
                "FFF" : 5200
                "GGG" : 5100
                "HHH" : 4200
                "III" : 4100
                "JJJ" : 2544
            ```
            """,

            """
            ## A nice chart here
            
            ```mermaid
            pie title Top 5 Analysts of Lab 33
                "kluciani" : 18982
                "arodriguez" : 18738
                "tsalgado" : 16440
                "ehunter" : 14952
                "thuynh" : 14802
            ```
            """,

            """
            # Cypher Query Example

            Here is a **Cypher query** for Neo4j:

            ```cypher
            MATCH (u:User)-[:FOLLOWS]->(other:User) WHERE u.name = "Alice" RETURN other.name, count(*) AS followers ORDER BY followers DESC LIMIT 5;
            ```
            """,

            """
            # A treem map
            
            ```mermaid
            treemap-beta
            "Category A"
              "55SELI62": 1742
              "55LEGR50": 1300
              "55ECOH45": 749
            "Category B"
              "55ATES44A": 487
              "55HEPA75": 452
              "55LEGR50A": 403
              "55OHEI93": 386
              "55MAPL78": 330
            "Category C"
              "55HEPA75A": 259
              "55SELI62D": 256
              "55PAEC50": 241
              "55AQSC25": 209
              "55PINC50": 208
            "Category D"
              "55ACTV75": 125
              "55TROW22": 124
              "55CERE25": 121
              "55HISG42": 120
              "55TGCL25": 112
            "Category E"
              "55SOCA75": 34
              "55GOLA62": 34
              "55STRU78": 33
              "55TKNK21": 33
              "55ANCL78": 33
            ```
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
            ## A nice chart here
            
            ```mermaid
            pie title Top 5 Analysts of Lab 33
                "kluciani" : 18982
                "arodriguez" : 18738
                "tsalgado" : 16440
                "ehunter" : 14952
                "thuynh" : 14802
            ```
            """,

            """
            # Table example

            | Column | Description |
            |--------|-------------|
            | A      | Alpha       |
            | B      | Beta        |
            """,

            """
            # Another header
            ## Sub header
            ### Sub sub header
            #### Sub sub sub header
            """,

            """
            # Some charts

            ## First a cool chart

            ```mermaid
            graph LR
                A["C-Lead by FLAA"] --> B["9607"]
                C["R006 NJ 48 Hr Gross Alpha"] --> D["5996"]
                E["C-Metals by ICP-MS"] --> F["4845"]
                G["C-Metals by ICP"] --> H["4313"]
                I["C-PFAS 3"] --> J["4167"]

                style A fill:#f96
                style C fill:#69f
                style E fill:#9f6
                style G fill:#f69
                style I fill:#ccf
            ```
            
            ## Then a pie chart

            ```mermaid
            pie title Top 5 Analysts of Lab 33
                "kluciani" : 18982
                "arodriguez" : 18738
                "tsalgado" : 16440
                "ehunter" : 14952
                "thuynh" : 14802
            ```
            """,

            """
            # Interesting table

            | Laboratory | OrderCount | SalesRepresentatives |
            |---|---|---|
            | Lab 04 | 14802 | [cbrandt, aderosa, jbish, jsilverman, pfrasca, nfrasca, jmonturano, gperlmutter, gegiazarov, ssteinmetz, pfrye, rjcavadini, dprince, jrucker, nmurphy, cmcmillan, jmcdonald, swiersgalla, kelley, estressman, jlafleur, kmcdonough, zbailey, jabels, DPRINCE, JSILVERMAN, nziko, clcutler, sjamieson, JMCDONALD, houseaccount, Unassigned, bmulcahy, pgriffin, SSTEINMETZ, asantangelo, jpassero] |
            | Lab 32 | 9292 | [houseaccount, cmcmillan, rjcavadini, nmurphy, kelley, cbrandt, pfrasca, swiersgalla, zbailey, jbish, ssteinmetz, bmulcahy, nfrasca, aderosa, jpassero, jmcdonald, kmcdonough, gperlmutter, Unassigned] |
            | Lab 55 | 8305 | [sjamieson, pfrasca, asantangelo, zbailey, pfrye, dprince, avretenar, Unassigned, houseaccount, kmcdonough, jbish, kmclean, cbrandt, nziko, ssteinmetz] |
            | Lab 16 | 7771 | [jbish, nmurphy, aderosa, cbrandt, gperlmutter, clcutler, jmcdonald, estressman, swiersgalla, jrucker, jmonturano, houseaccount, jabels, dprince, gegiazarov, cmcmillan, ssteinmetz, kmcdonough, jlafleur, Unassigned, jjacques, rjcavadini, pfrasca, kelley, nziko, JRUCKER, zbailey, nfrasca, pfrye, jsilverman] |
            | Lab 65 | 7277 | [sjamieson, dprince, houseaccount, kmclean, asantangelo, kmcdonough, pfrasca, zbailey, jbish, jrucker] |
            """
        ];

        foreach (var answer in answers)
        {
            if (ct.IsCancellationRequested) yield break;

            yield return answer;

            try
            {
                await Task.Delay(2_000, ct);
            }
            catch(OperationCanceledException)
            {
                continue;
            }
        }
    }

}
