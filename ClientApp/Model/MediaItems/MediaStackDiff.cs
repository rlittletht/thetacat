namespace Thetacat.Model;

public class MediaStackDiff
{
    public MediaStack.Op PendingOp;
    public int VectorClock;
    public MediaStack Stack;

    public MediaStackDiff(MediaStack stack, MediaStack.Op pendingOp)
    {
        VectorClock = stack.VectorClock;
        Stack = new MediaStack(stack);
        PendingOp = pendingOp;
    }
}
