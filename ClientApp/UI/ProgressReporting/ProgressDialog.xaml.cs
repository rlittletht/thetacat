using System;
using System.Threading.Tasks;
using System.Windows;

namespace Thetacat.UI;

public interface IProgressReport
{
    // update in percent (0.0 to 100.0)
    public void UpdateProgress(double progress);
    public void SetIndeterminate();
    public void WorkCompleted();
}

// This will perform the given work on a background thread
// and launch the modal progress dialog

public delegate void WorkDelegate(IProgressReport reportProgress);


/// <summary>
/// Interaction logic for ProgressDialog.xaml
/// </summary>
public partial class ProgressDialog : Window, IProgressReport
{
    private ProgressDialogModel Model = new();

    public void UpdateProgress(double progress)
    {
        int progressVal = (int)Math.Round(progress * 10);

        if (Model.ProgressValue != progressVal)
            Model.ProgressValue = progressVal;
    }

    public void WorkCompleted()
    {
        Model.ProgressValue = 1000;
        Application.Current.Dispatcher.Invoke(Close);
    }

    public void SetIndeterminate()
    {
        Model.IsIndeterminate = true;
    }

    public ProgressDialog()
    {
        InitializeComponent();
        DataContext = Model;
    }

    public static void DoWorkWithProgress(WorkDelegate work, Window? parent = null)
    {
        ProgressDialog dialog = new ProgressDialog();

        Task.Run(() => work(dialog));
        dialog.Owner = parent;
        dialog.ShowDialog();
    }
}