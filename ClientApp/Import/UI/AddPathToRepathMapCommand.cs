using System.Windows.Input;
using System;
using Thetacat.Logging;
using Thetacat.Explorer;
using Thetacat.Metatags;
using Thetacat.Types;

namespace Thetacat.Import.UI.Commands;

public delegate void AddPathToRepathMapDelegate(IBackingTreeItem? context);

public class AddPathToRepathMapCommand : ICommand
{
    private readonly AddPathToRepathMapDelegate RepathDelegate;

    public AddPathToRepathMapCommand(AddPathToRepathMapDelegate repathDelegate)
    {
        RepathDelegate = repathDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is IBackingTreeItem item)
            RepathDelegate(item);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}