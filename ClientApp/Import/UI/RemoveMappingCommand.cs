using System.Windows.Input;
using System;
using Thetacat.Logging;
using Thetacat.Explorer;
using Thetacat.Metatags;
using Thetacat.Types;

namespace Thetacat.Import.UI.Commands;

public delegate void RemoveMappingDelegate(RepathItem? context);

public class RemoveMappingCommand : ICommand
{
    private readonly RemoveMappingDelegate RepathDelegate;

    public RemoveMappingCommand(RemoveMappingDelegate repathDelegate)
    {
        RepathDelegate = repathDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is RepathItem item)
            RepathDelegate(item);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}