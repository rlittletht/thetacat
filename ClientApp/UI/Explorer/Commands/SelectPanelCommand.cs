using System.Windows.Input;
using System;
using Thetacat.Logging;

namespace Thetacat.UI.Explorer.Commands;

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

    public event EventHandler? CanExecuteChanged;
}