using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Thetacat.UI;

namespace Thetacat.Util;

// a collection of background workers. Each added item will have a notion of finishing
// each unit of work will be run independent of other work
public class BackgroundWorkers
{
    public delegate void StartFirstWorkerDelegate();
    public delegate void CompleteLastWorkerDelegate();

    public ObservableCollection<IBackgroundWorker> Workers { get; init; }

    private StartFirstWorkerDelegate? m_startFirst;
    private CompleteLastWorkerDelegate? m_completeLast;

    public BackgroundWorkers(StartFirstWorkerDelegate? startFirst = null, CompleteLastWorkerDelegate? completeLast = null)
    {
        Workers = new ObservableCollection<IBackgroundWorker>();
        m_startFirst = startFirst;
        m_completeLast = completeLast;
    }

    // you don't have to provide a progress report if you want this collection to be the only tracker
    public void AddWork<T>(
        string description, 
        BackgroundWorkerWork<T> work, 
        IProgressReport? progress = null, 
        OnWorkCompletedDelegate? onWorkCompleted = null)
    {
        BackgroundWorker<T> worker = new(description, work);

        if (onWorkCompleted != null)
            worker.BackgroundWorkCompleted += (sender, _) => onWorkCompleted((IBackgroundWorker)sender!);

        worker.BackgroundWorkCompleted += (sender, _) => OnWorkCompleted((IBackgroundWorker)sender!);

        if (Workers.Count == 0)
            m_startFirst?.Invoke();

        Workers.Add(worker);
        Task.Run(() => worker.Start(progress));
    }

    public async Task<T> DoWorkAsync<T>(string description, BackgroundWorkerWork<T> work, IProgressReport? progress = null)
    {
        BackgroundWorker<T> worker = new BackgroundWorker<T>(description, work);

        worker.BackgroundWorkCompleted += (sender, _) => OnWorkCompleted((IBackgroundWorker)sender!);

        if (Workers.Count == 0)
            m_startFirst?.Invoke();

        Workers.Add(worker);
        return await Task.Run(() => worker.Start(progress));
    }


    public void OnWorkCompleted(IBackgroundWorker worker)
    {
        ThreadContext.InvokeOnUiThread(()=>Workers.Remove(worker));
        if (Workers.Count == 0)
            m_completeLast?.Invoke();
    }
}
