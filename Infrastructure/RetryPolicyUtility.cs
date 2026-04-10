using System;
using System.Linq;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace Infrastructure;

internal class RetryPolicyUtility
{
    private static readonly Random _randomGenerator = new Random(256443);

    public static AsyncRetryPolicy GetLinearBackOffRetryPolicyForTransientGeminiErrorsAsync(int maxRetries,
            long millisecondsForEachRetry, Func<Exception, TimeSpan, int, ValueTask> onEachRetry) =>
            Policy
                .Handle<Google.GenAI.ServerError>(e => e.StatusCode == 503)
                .WaitAndRetryAsync(sleepDurations: GetLinearRetryTimes(
                    maxRetryLimit: maxRetries, millisecondsForEachRetry: millisecondsForEachRetry),
                    onRetry: async (ex, ts, cn, ct) => await onEachRetry(ex, ts, cn));

    public static AsyncRetryPolicy GetLinearBackOffRetryPolicyAsync<TException>(int maxRetries,
            long millisecondsForEachRetry, Action<Exception, TimeSpan, int> onEachRetry)
        where TException : Exception =>
            Policy
                .Handle<TException>()
                .WaitAndRetryAsync(sleepDurations: GetLinearRetryTimes(
                    maxRetryLimit: maxRetries, millisecondsForEachRetry: millisecondsForEachRetry),
                    onRetry: (ex, ts, cn, ct) => onEachRetry(ex, ts, cn));

    public static AsyncRetryPolicy GetExponentialBackOffRetryPolicyAsync<TException>(int maxRetries,
            Action<Exception, TimeSpan, int> onEachRetry)
        where TException : Exception =>
            Policy
                .Handle<TException>()
                .WaitAndRetryAsync(sleepDurations: GetExponentialRetryTimes(maxRetryLimit: maxRetries),
                    onRetry: (ex, ts, cn, ct) => onEachRetry(ex, ts, cn));

    public static AsyncRetryPolicy GetRandomBackOffRetryPolicyAsync<TException>(int maxRetries,
            Action<Exception, TimeSpan, int> onEachRetry)
        where TException : Exception =>
            Policy
                .Handle<TException>()
                .WaitAndRetryAsync(sleepDurations: GetRandomRetryTimes(maxRetryLimit: maxRetries),
                    onRetry: (ex, ts, cn, ct) => onEachRetry(ex, ts, cn));

    private static TimeSpan[] GetRandomRetryTimes(int maxRetryLimit) =>
        Enumerable.Range(1, maxRetryLimit)
            .Select(nthTime => RandomlyGetNextWaitForRetrySeconds())
            .ToArray();

    private static TimeSpan[] GetExponentialRetryTimes(int maxRetryLimit) =>
        Enumerable.Range(1, maxRetryLimit)
            .Select(GetRetrySecondsForNthTime)
            .ToArray();

    private static TimeSpan[] GetLinearRetryTimes(int maxRetryLimit, long millisecondsForEachRetry) =>
        Enumerable.Range(1, maxRetryLimit)
            .Select(r => TimeSpan.FromMilliseconds(millisecondsForEachRetry))
            .ToArray();

    private static TimeSpan GetRetrySecondsForNthTime(int nthTime) =>
        TimeSpan.FromSeconds(Math.Pow(2, nthTime - 1) / 3.0);

    private static TimeSpan RandomlyGetNextWaitForRetrySeconds()
    {
        var randomMultiplier = _randomGenerator.Next(1, 10);
        var millisecondsToWaitForNextRetry = (15.0 * randomMultiplier) / 1000.0;
        return TimeSpan.FromSeconds(millisecondsToWaitForNextRetry);
    }
}
