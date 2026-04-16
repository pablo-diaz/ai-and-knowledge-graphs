using Application.WorkflowOrchestrationLogic;
using Common;
using Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading;
using WebAppForWorkflows.Services;

namespace WebAppForWorkflows;

public class Program
{
    private class Neo4jConfig
    {
        public string Url { get; set; }
    }

    private class LlmApiKeysConfig
    {
        public string ForGemini { get; set; }
        public string ForOpenAI { get; set; }
    }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages();

        builder.Services.AddSingleton(_ => {
            var neo4jConfig = new Neo4jConfig();
            builder.Configuration.GetSection("Neo4jSettings").Bind(neo4jConfig);
            return new Neo4jService(url: neo4jConfig.Url);
        });

        builder.Services.AddKeyedSingleton(serviceKey: "FullLlm", implementationFactory: (sp, o) => {
            var llmApiKeysConfig = new LlmApiKeysConfig();
            builder.Configuration.GetSection("LlmApiKeys").Bind(llmApiKeysConfig);
            return new DecoratorForChatClient(
                provider: Constants.LlmProvider.Gemini,
                llmApiKey: llmApiKeysConfig.ForGemini,
                modelName: Constants.LlmModels.Gemini.FullModel);
        });

        builder.Services.AddKeyedSingleton(serviceKey: "LightLlm", implementationFactory: (sp, o) => {
            var llmApiKeysConfig = new LlmApiKeysConfig();
            builder.Configuration.GetSection("LlmApiKeys").Bind(llmApiKeysConfig);
            return new DecoratorForChatClient(
                provider: Constants.LlmProvider.Gemini,
                llmApiKey: llmApiKeysConfig.ForGemini,
                modelName: Constants.LlmModels.Gemini.LightModel);
        });

        builder.Services.AddSingleton<WorkflowSessionsHub>();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseRouting();

        //app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        app.MapGet("/events", ([FromQuery(Name = "sid")] string sessionId, [FromServices] WorkflowSessionsHub hub, CancellationToken ct) => {
            return string.IsNullOrEmpty(sessionId)
                ? Results.BadRequest("Missing Session Id query parameter.")
                : Results.Stream(
                    contentType: "text/event-stream",
                    streamWriterCallback: s => {
                        return hub.PutResponsesAsync(
                            ofSession: WorkflowSessionsHub.Session.From(id: sessionId),
                            intoStream: s,
                            ct: ct);
                    });
            });

        app.Run();
    }

}
