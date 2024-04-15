using System.Windows.Input;
using System;
using Thetacat.Logging;
using Thetacat.Explorer;
using Thetacat.Metatags;
using Thetacat.Types;
using Thetacat.Filtering.UI;

namespace Thetacat.Import.UI.Commands;

public delegate void RemoveInitialTagDelegate(FilterModelMetatagItem? context);

public class RemoveInitialTagCommand : ICommand
{
    private readonly RemoveInitialTagDelegate RemoveInitialTagDelegate;

    public RemoveInitialTagCommand(RemoveInitialTagDelegate removeInitialTagDelegate)
    {
        RemoveInitialTagDelegate = removeInitialTagDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is FilterModelMetatagItem item)
            RemoveInitialTagDelegate(item);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}