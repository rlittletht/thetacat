using System.Windows.Input;
using System;
using Thetacat.Logging;

namespace Thetacat.UI.Explorer.Commands;

public delegate void LaunchItemDelegate(MediaExplorerItem? context);

public class LaunchItemCommand : ICommand
{
    private readonly LaunchItemDelegate m_launchDelegate;

    public LaunchItemCommand(LaunchItemDelegate launchDelegate)
    {
        m_launchDelegate = launchDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is MediaExplorerItem item)
            m_launchDelegate(item);

        MainWindow.LogForApp(EventType.Information, $"Invoke LaunchItem");
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}