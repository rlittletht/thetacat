using System.Windows.Input;
using System;
using Thetacat.Explorer;

namespace Thetacat.Explorer.Commands;

public delegate void RotateItemsRightDelegate(MediaExplorerItem? context);

public class RotateItemsRightCommand : ICommand
{
    private readonly RotateItemsRightDelegate m_rotateItemsRightDelegate;

    public RotateItemsRightCommand(RotateItemsRightDelegate rotateItemsRightDelegate)
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