using System.Windows.Input;
using System;
using Thetacat.Logging;
using Thetacat.Explorer;
using Thetacat.Metatags;

namespace Thetacat.Explorer.Commands;

public delegate void DeleteMetatagDelegate(IMetatagTreeItem? context);

public class DeleteMetatagCommand : ICommand
{
    private readonly DeleteMetatagDelegate m_deleteDelegate;

    public DeleteMetatagCommand(DeleteMetatagDelegate deleteDelegate)
    {
        m_deleteDelegate = deleteDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is IMetatagTreeItem item)
            m_deleteDelegate(item);

        MainWindow.LogForApp(EventType.Information, $"Invoke DeleteMetatag");
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}