using System.Collections.Generic;
using Meziantou.Framework.WPF.Collections;
using Thetacat.Types.Parallel;

namespace Thetacat.Logging;

public class CatLog
{
    private EventType m_mostVerboseEventType = EventType.Error;

//    public ObservableImmutableList<ILogEntry> Entries = new ObservableImmutableList<ILogEntry>();
    public ConcurrentObservableCollection<ILogEntry> Entries = new ConcurrentObservableCollection<ILogEntry>();

    private static Dictionary<EventType, int> s_logLevelMap =
        new()
        {
            { EventType.Critical, 0 },
            { EventType.Error, 1 },
            { EventType.Warning, 2 },
            { EventType.Information, 3 },
            { EventType.Verbose, 4 }
        };

    public bool ShouldLog(EventType eventType)
    {
        return (s_logLevelMap[eventType] <= s_logLevelMap[m_mostVerboseEventType]);
    }

    public void Log(ILogEntry entry)
    {
        if (ShouldLog(entry.EventType))
            Entries.Add(entry);
    }

    public CatLog(EventType mostVerboseEventType)
    {
        m_mostVerboseEventType = mostVerboseEventType;
    }

    public CatLog()
    {
    }
}
