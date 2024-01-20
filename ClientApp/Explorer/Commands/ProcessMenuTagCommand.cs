using System.Windows.Input;
using System;
using Thetacat.Logging;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void ProcessMenuTagDelegate(ExplorerMenuTag? context);

public class ProcessMenuTagCommand : ICommand
{
    private readonly ProcessMenuTagDelegate m_processMenuTagDelegate;

    public ProcessMenuTagCommand(ProcessMenuTagDelegate processMenuTagDelegate)
    {
        m_processMenuTagDelegate = processMenuTagDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is ExplorerMenuTag item)
            m_processMenuTagDelegate(item);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}