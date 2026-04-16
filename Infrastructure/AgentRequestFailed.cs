using System;

namespace Infrastructure;

public static class AgentRequestFailed
{
    public sealed class TransientError: Exception
    {
        public TransientError(Exception inner) : base($"A transient AI service error occurred. {inner.Message}", inner)
        {
        }
    }

    public sealed class ServiceQuotaSurpassed : Exception
    {
        public ServiceQuotaSurpassed(Exception inner) : base($"AI service Quota has been surpassed. {inner.Message}", inner)
        {
        }
    }

    public static Exception Create(Exception fromException, Common.Constants.LlmProvider usingProvider)
    {
        if(usingProvider == Common.Constants.LlmProvider.Gemini
            && fromException is Google.GenAI.ServerError geminiError
            && geminiError.StatusCode == 503)
            return new TransientError(fromException);

        if (usingProvider == Common.Constants.LlmProvider.OpenAI
            && fromException is System.ClientModel.ClientResultException clientEx
            && clientEx.Status == 429)
            return new ServiceQuotaSurpassed(fromException);

        return fromException;
    }

}
