using Thetacat.TCore.TcSqlLite;

namespace Tests.Model.Sql;

public class SqlCommandSim:  ISqlCommand
{
    private readonly Dictionary<string, object>? m_expectedParameters;

    public SqlCommandSim(Dictionary<string, object>? expectedParameters)
    {
        m_expectedParameters = expectedParameters;
        CommandText = string.Empty;
    }

    public string CommandText { get; set; }
    public ISqlTransaction? Transaction { get; set; }
    public ISqlReader ExecuteReader() => throw new NotImplementedException();

    public int ExecuteNonQuery() => throw new NotImplementedException();

    public object ExecuteScalar() => throw new NotImplementedException();

    public void AddParameterWithValue(string parameterName, object? value)
    {
        if (m_expectedParameters == null)
            throw new InvalidOperationException($"no expected parameters for {parameterName}");

        if (!m_expectedParameters.ContainsKey(parameterName))
            throw new InvalidOperationException($"expected parameters not found: {parameterName}");

        Assert.AreEqual(m_expectedParameters[parameterName], value);
    }

    public void Close()
    {
        throw new NotImplementedException();
    }
}
