using System.Windows.Input;
using System;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void ShowHideMetatagPanelDelegate(MediaExplorerItem? context);

public class ShowHideMetatagPanelCommand : ICommand
{
    private readonly ShowHideMetatagPanelDelegate m_showHideDelegate;

    public ShowHideMetatagPanelCommand(ShowHideMetatagPanelDelegate showHideDelegate)
    {
        m_showHideDelegate = showHideDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        m_showHideDelegate(parameter as MediaExplorerItem);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}