using System;
using System.ComponentModel;

namespace Thetacat.Logging;

public interface ILogEntry : INotifyPropertyChanged
{
    public EventType EventType { get; set; }
    public string Summary { get; set; }
    public string Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string CorrelationID { get; set; }
}
