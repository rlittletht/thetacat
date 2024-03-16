using System.Windows.Input;
using System;
using Thetacat.Logging;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void SelectPanelDelegate(MediaExplorerItem? context);

public class SelectPanelCommand : ICommand
{
    private readonly SelectPanelDelegate m_selectDelegate;

    public SelectPanelCommand(SelectPanelDelegate selectDelegate)
    {
        m_selectDelegate = selectDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is MediaExplorerItem item)
            m_selectDelegate(item);

        MainWindow.LogForApp(EventType.Information, $"Invoke SelectPanel");
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}