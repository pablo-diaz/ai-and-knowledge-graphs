namespace Common;

public class Constants
{
    public enum LlmProvider
    {
        Gemini,
        OpenAI
    }

    public static class LlmModels
    {
        public static class Gemini
        {
            public const string FullModel = "gemini-3-flash-preview";  // better model, that knows about cypher and graph databases, to generate the query from the question
            public const string LightModel = "gemini-3.1-flash-lite-preview"; // different model, to answer very simple questions
        }

        public static class OpenAI
        {
            public const string FullModel = "o4-mini"; // better model, that knows about cypher and graph databases, to generate the query from the question
            public const string LightModel = "o3"; // different model, to answer very simple questions
        }
    }

}
