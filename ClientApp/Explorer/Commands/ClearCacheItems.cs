using System.Windows.Input;
using System;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void ClearCacheItemsDelegate(MediaExplorerItem? context);

public class ClearCacheItemsCommand : ICommand
{
    private readonly ClearCacheItemsDelegate m_clearCacheItemsDelegate;

    public ClearCacheItemsCommand(ClearCacheItemsDelegate clearCacheItemsDelegate)
    {
        m_clearCacheItemsDelegate = clearCacheItemsDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        m_clearCacheItemsDelegate(parameter as MediaExplorerItem);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}