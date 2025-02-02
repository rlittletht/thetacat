using System.Windows.Input;
using System;
using Thetacat.Logging;
using Thetacat.Explorer;
using Thetacat.Metatags;
using Thetacat.Types;

namespace Thetacat.Import.UI.Commands;

public delegate void SetMediaTagValueDelegate(IMetatagTreeItem? context);

public class SetMediaTagValueCommand : ICommand
{
    private readonly SetMediaTagValueDelegate SetMediaTagValueDelegate;

    public SetMediaTagValueCommand(SetMediaTagValueDelegate del)
    {
        SetMediaTagValueDelegate = del;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is IMetatagTreeItem item)
            SetMediaTagValueDelegate(item);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}