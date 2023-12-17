namespace Thetacat.TCore.TcSqlLite;

public delegate void AddParameterWithValueDelegate(string parameter, object value);
public delegate void CustomizeCommandDelegate(AddParameterWithValueDelegate addDelegate);

public interface ISqlCommand
{
    public string CommandText { get; set; }
    public T GetTransaction<T>();
    public void SetTransaction<T>(T transaction);
    public TReader ExecuteReader<TReader>();
    public int ExecuteNonQuery();
    public object ExecuteScalar();
    public void AddParameterWithValue(string parameterName, object value);
}
