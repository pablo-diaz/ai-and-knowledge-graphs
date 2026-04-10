using System.Threading.Tasks;

using Common;

using Google.GenAI;
using Google.GenAI.Types;
using Newtonsoft.Json;

namespace Infrastructure;

public class GeminiService: ILargeLanguageModel
{
    private readonly Client _client;

    public GeminiService(string apiKey)
    {
        _client = GetChatClient(withApiKey: apiKey);
    }

    public async Task<TExpectedStructure> GetStructuredResponse<TExpectedStructure>(string withUserMessage, string withSystemPrompt, string usingJsonStructuredOutputSchema)
    {
        /*var responseSchema = new Schema {
            Type = "object",
            Properties = new System.Collections.Generic.Dictionary<string, Schema> { { "cypherQuery", new Schema { Type = "string" } } },
            Required = ["cypherQuery"]
        };*/

        var response = await _client.Models.GenerateContentAsync(
            model: Constants.LlmModels.Gemini.FullModel,
            config: new GenerateContentConfig()
            {
                SystemInstruction = new Content() { Parts = [Part.FromText(withSystemPrompt)] },
                
                //ResponseSchema = responseSchema
                
                ResponseJsonSchema = JsonConvert.DeserializeObject<Schema>(usingJsonStructuredOutputSchema),
                ResponseMimeType = "application/json"
            },
            contents: withUserMessage);

        return JsonConvert.DeserializeObject<TExpectedStructure>(response.Text);
    }

    public async Task<string> GetBasicResponse(string withUserMessage)
    {
        var response = await _client.Models.GenerateContentAsync(
            model: Constants.LlmModels.Gemini.LightModel,
            contents: withUserMessage);

        return response.Text;
    }

    private static Client GetChatClient(string withApiKey) =>
        new Client(apiKey: withApiKey);

    public void Dispose()
    {
        _client?.Dispose();
    }

}
