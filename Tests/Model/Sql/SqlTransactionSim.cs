
using TCore.SqlCore;

namespace Tests.Model.Sql;

public class SqlTransactionSim: ISqlTransaction
{
    public void Rollback()
    {
        throw new NotImplementedException();
    }

    public void Commit()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
