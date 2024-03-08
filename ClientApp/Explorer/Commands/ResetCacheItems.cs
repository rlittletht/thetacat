using System.Windows.Input;
using System;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void ResetCacheItemsDelegate(MediaExplorerItem? context);

public class ResetCacheItemsCommand : ICommand
{
    private readonly ResetCacheItemsDelegate m_resetCacheItemsDelegate;

    public ResetCacheItemsCommand(ResetCacheItemsDelegate resetCacheItemsDelegate)
    {
        m_resetCacheItemsDelegate = resetCacheItemsDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        m_resetCacheItemsDelegate(parameter as MediaExplorerItem);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}