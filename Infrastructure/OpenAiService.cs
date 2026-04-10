using System;
using System.Text;
using System.Threading.Tasks;

using Common;

using OpenAI;
using OpenAI.Chat;

namespace Infrastructure;

public class OpenAiService: ILargeLanguageModel
{
    private readonly OpenAIClient _client;

    public OpenAiService(string apiKey)
    {
        _client = new OpenAIClient(apiKey: apiKey);
    }

    public async Task<TExpectedStructure> GetStructuredResponse<TExpectedStructure>(string withUserMessage, string withSystemPrompt, string usingJsonStructuredOutputSchema)
    {
        var jsonSchema = BinaryData.FromBytes(Encoding.UTF8.GetBytes(usingJsonStructuredOutputSchema));

        var response = await _client
            .GetChatClient(model: Constants.LlmModels.OpenAI.FullModel)
            .CompleteChatAsync(messages: [
                    new SystemChatMessage(withSystemPrompt),
                    new UserChatMessage(withUserMessage)
                ],
                options: new() {
                    ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                        jsonSchemaFormatName: "cypherPurposed", jsonSchema: jsonSchema, jsonSchemaIsStrict: true)
                }
            );

        return Newtonsoft.Json.JsonConvert.DeserializeObject<TExpectedStructure>(response.Value.Content[0].Text);
    }

    public async Task<string> GetBasicResponse(string withUserMessage)
    {
        var response = await _client
            .GetChatClient(model: Constants.LlmModels.OpenAI.LightModel)
            .CompleteChatAsync(messages: new UserChatMessage(withUserMessage));

        return response.Value.Content[0].Text;
    }

    public void Dispose()
    {
        // this client does not get disposed, but if it did, we would dispose it here
    }

}
