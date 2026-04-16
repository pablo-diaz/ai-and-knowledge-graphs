using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure;

public static class AgentUtilities
{
    public delegate ValueTask<string> CallAgentFunction(CancellationToken cancellationToken);
    public delegate ValueTask OnEachRetryFunction(Exception ExceptionCaught, TimeSpan TimeToWaitBeforeNextRetry, int RetryNumber);

    public sealed record AgentCallingSettings(CallAgentFunction fnCallAgentAsync, int MaxRetries, long EveryMilliseconds, OnEachRetryFunction OnEachRetry);

    public static async Task<string> ResilientlyCallAgent(AgentCallingSettings settings, CancellationToken cancellationToken)
    {
        var retryPolicy = RetryPolicyUtility.GetLinearBackOffRetryPolicy<AgentRequestFailed.TransientError>(
            maxRetries: settings.MaxRetries,
            millisecondsForEachRetry: settings.EveryMilliseconds,
            onEachRetry: async (e, t, r) => await settings.OnEachRetry(e, t, r));

        try
        {
            var overallExecution = await retryPolicy.ExecuteAndCaptureAsync(
                action: async _ => await settings.fnCallAgentAsync(cancellationToken),
                cancellationToken: cancellationToken);

            return overallExecution.Outcome == OutcomeType.Successful
                ? overallExecution.Result
                : ThrowException(fromResult: overallExecution);
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }

    private static string ThrowException<T>(PolicyResult<T> fromResult)
    {
        throw new ApplicationException(
            message: fromResult.FinalException?.Message ?? "Unknown error occurred while calling the agent.",
            innerException: fromResult.FinalException);
    }

}
