using TCore.SqlCore;

namespace Tests.Model.Sql;

public class SqlSimNonQueryDataItem
{
    public delegate bool FMatchDelegate(string query);
    public delegate void ValidateDelegate(string query);

    private readonly FMatchDelegate m_matchDelegate;
    private readonly ValidateDelegate m_validateDelegate;

    public bool FMatch(string query) => m_matchDelegate(query);
    public void Validate(string query) => m_validateDelegate(query);
    public ISqlCommand? CommandExpected { get; set; }
    public bool RemoveAfterMatch { get; }

    public SqlSimNonQueryDataItem(FMatchDelegate matchDelegate, ValidateDelegate validate, ISqlCommand? commandExpected = null, bool removeAfterMatch = true)
    {
        m_validateDelegate = validate;
        m_matchDelegate = matchDelegate;
        RemoveAfterMatch = removeAfterMatch;
        CommandExpected = commandExpected;
    }
}
