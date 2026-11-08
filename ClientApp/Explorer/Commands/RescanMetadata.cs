using System.Windows.Input;
using System;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void RescanMetadataDelegate(MediaExplorerItem? context);

public class RescanMetadataCommand : ICommand
{
    private readonly RescanMetadataDelegate m_resetCacheItemsDelegate;

    public RescanMetadataCommand(RescanMetadataDelegate resetCacheItemsDelegate)
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