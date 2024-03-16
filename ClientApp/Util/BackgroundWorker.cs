using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Model.ImageCaching;
using Thetacat.UI;

namespace Thetacat.Util;

public delegate T BackgroundWorkerWork<T>(IProgressReport progress);
public delegate void OnWorkCompletedDelegate(IBackgroundWorker worker);

// a single background worker created to do a specific work item. This doesn't
// handle any threading issues, that's the job of the BackgroundWorkers object
public class BackgroundWorker<T>: INotifyPropertyChanged, IProgressReport, IBackgroundWorker
{
    private int m_tenthPercentComplete;
    private string m_description;

    public event EventHandler<BackgroundWorkCompletedEventArgs>? BackgroundWorkCompleted;
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

    private readonly BackgroundWorkerWork<T> m_work;

    public BackgroundWorker(string description, BackgroundWorkerWork<T> work)
    {
        m_description = description;
        m_work = work;
    }

    private void TriggerBackgroundWorkCompleted()
    {
        if (BackgroundWorkCompleted != null)
            BackgroundWorkCompleted(this, new BackgroundWorkCompletedEventArgs());
    }

    private IProgressReport? m_progressInner;
    private bool m_isIndeterminate;

    public T Start(IProgressReport? progress)
    {
        m_progressInner = progress;
        T t = m_work(this);
        TriggerBackgroundWorkCompleted();

        return t;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T1>(ref T1 field, T1 value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T1>.Default.Equals(field, value)) return false;
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
