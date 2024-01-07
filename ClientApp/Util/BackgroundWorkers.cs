using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Thetacat.UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace Thetacat.Util;

// a collection of background workers. Each added item will have a notion of finishing
public class BackgroundWorkers
{
    public delegate void StartFirstWorkerDelegate();
    public delegate void CompleteLastWorkerDelegate();

    public ObservableCollection<BackgroundWorker> Workers { get; init; }

    private StartFirstWorkerDelegate? m_startFirst;
    private CompleteLastWorkerDelegate? m_completeLast;

    public BackgroundWorkers(StartFirstWorkerDelegate? startFirst = null, CompleteLastWorkerDelegate? completeLast = null)
    {
        Workers = new ObservableCollection<BackgroundWorker>();
        m_startFirst = startFirst;
        m_completeLast = completeLast;
    }

    // you don't have to provide a progress report if you want this collection to be the only tracker
    public void AddWork(string description, BackgroundWorkerWork work, IProgressReport? progress = null)
    {
        BackgroundWorker worker = new BackgroundWorker(description, work, OnWorkCompleted);

        if (Workers.Count == 0)
            m_startFirst?.Invoke();

        Workers.Add(worker);
        Task.Run(() => worker.Start(progress));
    }

    public void OnWorkCompleted(BackgroundWorker worker)
    {
        ThreadContext.InvokeOnUiThread(()=>Workers.Remove(worker));
        if (Workers.Count == 0)
            m_completeLast?.Invoke();
    }
}
