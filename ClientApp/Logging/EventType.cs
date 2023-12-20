namespace Thetacat.Logging;
using System.Diagnostics;

public enum EventType
{
    Critical = TraceEventType.Critical,
    Error = TraceEventType.Error,
    Warning = TraceEventType.Warning,
    Information = TraceEventType.Information,
    Verbose = TraceEventType.Verbose
}

