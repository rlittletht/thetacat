using System.Windows.Input;
using System;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void ToggleTopOfStackDelegate(MediaExplorerItem? context);

public class ToggleTopOfStackCommand : ICommand
{
    private readonly ToggleTopOfStackDelegate m_toggleTopOfStackDelegate;

    public ToggleTopOfStackCommand(ToggleTopOfStackDelegate toggleTopOfStackDelegate)
    {
        m_toggleTopOfStackDelegate = toggleTopOfStackDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        m_toggleTopOfStackDelegate(parameter as MediaExplorerItem);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}