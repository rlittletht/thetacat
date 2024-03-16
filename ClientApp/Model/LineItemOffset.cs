namespace Thetacat.Model;

public class LineItemOffset
{
    public int Line { get; set; }
    public int Offset { get; set; }

    public LineItemOffset(int line, int offset)
    {
        Line = line;
        Offset = offset;
    }

    public LineItemOffset(LineItemOffset offset)
    {
        Line = offset.Line;
        Offset = offset.Offset;
    }
}
