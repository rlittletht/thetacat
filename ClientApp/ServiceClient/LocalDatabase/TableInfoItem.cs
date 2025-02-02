namespace Thetacat.ServiceClient.LocalDatabase;

public class TableInfoItem
{
    public int Cid;
    public string Name;
    public string Type;
    public int NotNull;
    public string? DefaultValue;
    public int PK;

    public TableInfoItem(int cid, string name, string type, int notNull, string? defaultValue, int pK)
    {
        Cid = cid;
        Name = name;
        Type = type;
        NotNull = notNull;
        DefaultValue = defaultValue;
        PK = pK;
    }
}

