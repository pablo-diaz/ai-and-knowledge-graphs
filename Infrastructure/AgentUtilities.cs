using System;
using System.Threading.Tasks;

namespace Infrastructure;

public static class AgentUtilities
{
    public delegate ValueTask<string> CallAgentFunction();
    public delegate ValueTask OnEachRetryFunction(Exception ExceptionCaught, TimeSpan TimeToWaitBeforeNextRetry, int RetryNumber);

    public sealed record GeminiAgentCallSettings(CallAgentFunction fnCallAgent, int MaxRetries, long EveryMilliseconds, OnEachRetryFunction OnEachRetry);

    public static async ValueTask<string> ResilientlyCallGeminiAgent(GeminiAgentCallSettings settings)
    {
        var retryPolicy = RetryPolicyUtility.GetLinearBackOffRetryPolicyForTransientGeminiErrorsAsync(
            maxRetries: settings.MaxRetries,
            millisecondsForEachRetry: settings.EveryMilliseconds,
            onEachRetry: async (e, t, r) => await settings.OnEachRetry(e, t, r));

        return await retryPolicy.ExecuteAsync(async () => {
            return await settings.fnCallAgent();
        });
    }

}
