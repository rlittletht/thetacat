using System.Windows.Input;
using System;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void MirrorItemsDelegate(MediaExplorerItem? context);

public class MirrorItemsCommand : ICommand
{
    private readonly MirrorItemsDelegate m_rotateItemsRightDelegate;

    public MirrorItemsCommand(MirrorItemsDelegate rotateItemsRightDelegate)
    {
        m_rotateItemsRightDelegate = rotateItemsRightDelegate;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        m_rotateItemsRightDelegate(parameter as MediaExplorerItem);
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}