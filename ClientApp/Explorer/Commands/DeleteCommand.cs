using System.Windows.Input;
using System;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void DeleteDelegate(MediaExplorerItem? context);

public class DeleteCommand : ICommand
{
    private readonly DeleteDelegate m_deleteDelegate;

    public DeleteCommand(DeleteDelegate deleteDelegate)
    {
        m_deleteDelegate = deleteDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        m_deleteDelegate(parameter as MediaExplorerItem);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}