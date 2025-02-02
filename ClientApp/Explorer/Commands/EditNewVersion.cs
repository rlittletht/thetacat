using System.Windows.Input;
using System;
using Thetacat.Logging;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void EditNewVersionDelegate(MediaExplorerItem? context);

public class EditNewVersionCommand : ICommand
{
    private readonly EditNewVersionDelegate m_editNewVersionDelegate;

    public EditNewVersionCommand(EditNewVersionDelegate editNewVersionDelegate)
    {
        m_editNewVersionDelegate = editNewVersionDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is MediaExplorerItem item)
            m_editNewVersionDelegate(item);

        App.LogForApp(EventType.Verbose, $"Invoke EditNewVersion");
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}