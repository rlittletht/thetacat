using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.Logging;

public class LogEntry: ILogEntry
{
    private EventType m_eventType;
    private string m_summary;
    private string m_details;
    private DateTime m_timestamp;
    private string m_correlationId;

    public EventType EventType
    {
        get => m_eventType;
        set => SetField(ref m_eventType, value);
    }

    public string Summary
    {
        get => m_summary;
        set => SetField(ref m_summary, value);
    }

    public string Details
    {
        get => m_details;
        set => SetField(ref m_details, value);
    }

    public DateTime Timestamp
    {
        get => m_timestamp;
        set => SetField(ref m_timestamp, value);
    }

    public string CorrelationID
    {
        get => m_correlationId;
        set => SetField(ref m_correlationId, value);
    }

    public LogEntry(EventType eventType, string summary, string correlationId = "", string? details = null)
    {
        m_eventType = eventType;
        m_summary = summary;
        m_details = details ?? string.Empty;
        m_correlationId = correlationId;
        m_timestamp = DateTime.Now;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
