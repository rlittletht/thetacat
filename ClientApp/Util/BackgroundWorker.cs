using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.UI;

namespace Thetacat.Util;

public delegate void BackgroundWorkerWork(IProgressReport progress);
public delegate void OnWorkCompletedDelegate(BackgroundWorker worker);

// a single background worker created to do a specific work item. This doesn't
// handle any threading issues, that's the job of the BackgroundWorkers object
public class BackgroundWorker: INotifyPropertyChanged, IProgressReport
{
    private int m_tenthPercentComplete;
    private string m_description;

    public string Description
    {
        get => m_description;
        set => SetField(ref m_description, value);
    }

    public bool IsIndeterminate
    {
        get => m_isIndeterminate;
        set => SetField(ref m_isIndeterminate, value);
    }

    public int TenthPercentComplete
    {
        get => m_tenthPercentComplete;
        set => SetField(ref m_tenthPercentComplete, value);
    }

    private readonly BackgroundWorkerWork m_work;
    private readonly OnWorkCompletedDelegate? m_onWorkCompleted;

    public BackgroundWorker(string description, BackgroundWorkerWork work, OnWorkCompletedDelegate? onWorkComplete)
    {
        m_description = description;
        m_work = work;
        m_onWorkCompleted = onWorkComplete;
    }

    private IProgressReport? m_progressInner;
    private bool m_isIndeterminate;

    public void Start(IProgressReport? progress)
    {
        m_progressInner = progress;
        m_work(this);
        m_onWorkCompleted?.Invoke(this);
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

    public void UpdateProgress(double progress)
    {
        TenthPercentComplete = (int)Math.Round((progress * 10));
        m_progressInner?.UpdateProgress(progress);
    }

    public void WorkCompleted()
    {
        m_progressInner?.WorkCompleted();
    }

    public void SetIndeterminate()
    {
        IsIndeterminate = true;
    }
}
