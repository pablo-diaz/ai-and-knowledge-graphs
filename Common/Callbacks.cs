using System.Threading.Tasks;

namespace Common;

public class Callbacks
{
    public abstract record ProgressUpdate;
    
    public sealed record Information(string Description) : ProgressUpdate
    {
        public override string ToString() => Description;
    }

    public sealed record Error(string Description) : ProgressUpdate
    {
        public override string ToString() => Description;
    }

    public sealed record CypherQueryToExecute(string Query) : ProgressUpdate
    {
        public override string ToString() => $"\nCypher query to run:\n{Query}\n";
    }
    
    public sealed record FinalUserResponse(string Content) : ProgressUpdate
    {
        public override string ToString() => $"\nFinal answer:\n {Content}\n";
    }

    public delegate Task ReportProgress(ProgressUpdate Progress);
}
