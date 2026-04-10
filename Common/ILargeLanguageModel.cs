using System;
using System.Threading.Tasks;

namespace Common;

public interface ILargeLanguageModel: IDisposable
{
    Task<TExpectedStructure> GetStructuredResponse<TExpectedStructure>(string withUserMessage, string withSystemPrompt, string usingJsonStructuredOutputSchema);
    Task<string> GetBasicResponse(string withUserMessage);
}
