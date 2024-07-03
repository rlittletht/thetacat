using System.Windows.Input;
using System;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void OpenItemsStackDelegate(MediaExplorerItem? context);

public class OpenItemsStackCommand : ICommand
{
    private readonly OpenItemsStackDelegate m_openItemsStackDelegate;

    public OpenItemsStackCommand(OpenItemsStackDelegate openItemsStackDelegate)
    {
        m_openItemsStackDelegate = openItemsStackDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        m_openItemsStackDelegate(parameter as MediaExplorerItem);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}