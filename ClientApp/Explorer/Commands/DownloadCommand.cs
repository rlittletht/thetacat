using System.Windows.Input;
using System;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void DownloadDelegate(MediaExplorerItem? context);

public class DownloadCommand : ICommand
{
    private readonly DownloadDelegate m_downloadDelegate;

    public DownloadCommand(DownloadDelegate downloadDelegate)
    {
        m_downloadDelegate = downloadDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        m_downloadDelegate(parameter as MediaExplorerItem);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}