using Meziantou.Framework.WPF.Collections;
using Thetacat.Types.Parallel;

namespace Thetacat.Logging;

public class CatLog
{
//    public ObservableImmutableList<ILogEntry> Entries = new ObservableImmutableList<ILogEntry>();
    public ConcurrentObservableCollection<ILogEntry> Entries = new ConcurrentObservableCollection<ILogEntry>();

    public void Log(ILogEntry entry)
    {
        Entries.Add(entry);
    }
}
