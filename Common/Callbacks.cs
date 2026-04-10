using System.Threading.Tasks;

namespace Common;

public class Callbacks
{
    public delegate Task ReportProgress(string Progress);
}
