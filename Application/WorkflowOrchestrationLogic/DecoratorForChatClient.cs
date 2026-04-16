using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Common;

using Microsoft.Extensions.AI;

namespace Application.WorkflowOrchestrationLogic;

public sealed class DecoratorForChatClient : IChatClient
{
    private readonly IChatClient _innerChatClient;
    private readonly Constants.LlmProvider _provider;

    public DecoratorForChatClient(Constants.LlmProvider provider, string llmApiKey, string modelName)
    {
        this._provider = provider;
        if (provider == Constants.LlmProvider.Gemini)
        {
            _innerChatClient = new Google.GenAI.Client(apiKey: llmApiKey)
                .AsIChatClient(defaultModelId: modelName);
        }
        else if (provider == Constants.LlmProvider.OpenAI)
        { 
            _innerChatClient = new OpenAI.OpenAIClient(apiKey: llmApiKey)
                .GetChatClient(model: modelName)
                .AsIChatClient();
        }
        else
            throw new NotSupportedException($"The provider {provider} is not supported");
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _innerChatClient.GetResponseAsync(messages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            throw Infrastructure.AgentRequestFailed.Create(fromException: ex, usingProvider: _provider);
        }
    }

    public object GetService(Type serviceType, object serviceKey = null) =>
        _innerChatClient.GetService(serviceType, serviceKey);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions options = null, CancellationToken cancellationToken = default) =>
        _innerChatClient.GetStreamingResponseAsync(messages, options, cancellationToken);

    public void Dispose()
    {
        _innerChatClient?.Dispose();
    }

}
