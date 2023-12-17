using System.Data.SQLite;

namespace Thetacat.TCore.TcSqlLite;

public class SQLiteCommand : ISqlCommand
{
    private readonly System.Data.SQLite.SQLiteCommand m_command;

    public string CommandText
    {
        get => m_command.CommandText;
        set => m_command.CommandText = value;
    }

    public SQLiteTransaction GetTransaction<SQLiteTransaction>()
    {
        return m_command.Transaction;
    }

    public void SetTransaction(SQLiteTransaction transaction) => m_command.Transaction = transaction;

    public SQLiteDataReader ExecuteReader() => m_command.ExecuteReader();

    public SQLiteCommand(System.Data.SQLite.SQLiteCommand command)
    {
        m_command = command;
    }

    public int ExecuteNonQuery() => m_command.ExecuteNonQuery();

    public object ExecuteScalar() => m_command.ExecuteScalar();

    public void AddParameterWithValue(string parameterName, object value) => m_command.Parameters.AddWithValue(parameterName, value);
}
