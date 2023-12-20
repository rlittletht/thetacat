using Thetacat.Types.Parallel;

namespace Thetacat.Logging;

public class CatLog
{
    public ObservableImmutableList<ILogEntry> Entries = new ObservableImmutableList<ILogEntry>();

    public void Log(ILogEntry entry)
    {
        Entries.Add(entry);
    }
}
