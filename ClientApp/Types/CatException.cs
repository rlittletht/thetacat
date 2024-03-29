﻿using System;
using TCore.Exceptions;

namespace Thetacat.Types;

    // Basic exception for our WebApi to allow us to differentiate exceptions
    public class CatException : TcException
    {
#pragma warning disable format // @formatter:off
        public CatException() : base(Guid.Empty) { }
        public CatException(Guid crids) : base(crids) { }
        public CatException(string errorMessage) : base(errorMessage) { }
        public CatException(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatException(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatException(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
    }

    public class CatExceptionAuthenticationFailure : CatException
    {
#pragma warning disable format // @formatter:off
        public CatExceptionAuthenticationFailure() : base(Guid.Empty) { }
        public CatExceptionAuthenticationFailure(Guid crids) : base(crids) { }
        public CatExceptionAuthenticationFailure(string errorMessage) : base(errorMessage) { }
        public CatExceptionAuthenticationFailure(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionAuthenticationFailure(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionAuthenticationFailure(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionUnauthorized : CatException
    {
#pragma warning disable format // @formatter:off
        public CatExceptionUnauthorized() : base(Guid.Empty) { }
        public CatExceptionUnauthorized(Guid crids) : base(crids) { }
        public CatExceptionUnauthorized(string errorMessage) : base(errorMessage) { }
        public CatExceptionUnauthorized(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionUnauthorized(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionUnauthorized(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionWorkgroupNotFound : CatException
{
#pragma warning disable format // @formatter:off
        public CatExceptionWorkgroupNotFound() : base(Guid.Empty) { }
        public CatExceptionWorkgroupNotFound(Guid crids) : base(crids) { }
        public CatExceptionWorkgroupNotFound(string errorMessage) : base(errorMessage) { }
        public CatExceptionWorkgroupNotFound(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionWorkgroupNotFound(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionWorkgroupNotFound(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionServiceDataFailure : CatException
{
#pragma warning disable format // @formatter:off
        public CatExceptionServiceDataFailure() : base(Guid.Empty) { }
        public CatExceptionServiceDataFailure(Guid crids) : base(crids) { }
        public CatExceptionServiceDataFailure(string errorMessage) : base(errorMessage) { }
        public CatExceptionServiceDataFailure(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionServiceDataFailure(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionServiceDataFailure(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionDataCoherencyFailure : CatException
{
#pragma warning disable format // @formatter:off
        public CatExceptionDataCoherencyFailure() : base(Guid.Empty) { }
        public CatExceptionDataCoherencyFailure(Guid crids) : base(crids) { }
        public CatExceptionDataCoherencyFailure(string errorMessage) : base(errorMessage) { }
        public CatExceptionDataCoherencyFailure(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionDataCoherencyFailure(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionDataCoherencyFailure(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionInitializationFailure : CatException
{
#pragma warning disable format // @formatter:off
        public CatExceptionInitializationFailure() : base(Guid.Empty) { }
        public CatExceptionInitializationFailure(Guid crids) : base(crids) { }
        public CatExceptionInitializationFailure(string errorMessage) : base(errorMessage) { }
        public CatExceptionInitializationFailure(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionInitializationFailure(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionInitializationFailure(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionAzureFailure : CatException
{
#pragma warning disable format // @formatter:off
        public CatExceptionAzureFailure() : base(Guid.Empty) { }
        public CatExceptionAzureFailure(Guid crids) : base(crids) { }
        public CatExceptionAzureFailure(string errorMessage) : base(errorMessage) { }
        public CatExceptionAzureFailure(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionAzureFailure(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionAzureFailure(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionInternalFailure : CatException
{
#pragma warning disable format // @formatter:off
        public CatExceptionInternalFailure() : base(Guid.Empty) { }
        public CatExceptionInternalFailure(Guid crids) : base(crids) { }
        public CatExceptionInternalFailure(string errorMessage) : base(errorMessage) { }
        public CatExceptionInternalFailure(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionInternalFailure(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionInternalFailure(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionCanceled: CatException
{
#pragma warning disable format // @formatter:off
        public CatExceptionCanceled() : base(Guid.Empty) { }
        public CatExceptionCanceled(Guid crids) : base(crids) { }
        public CatExceptionCanceled(string errorMessage) : base(errorMessage) { }
        public CatExceptionCanceled(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionCanceled(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionCanceled(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionDatabaseLockTimeout : CatException
{
#pragma warning disable format // @formatter:off
        public CatExceptionDatabaseLockTimeout() : base(Guid.Empty) { }
        public CatExceptionDatabaseLockTimeout(Guid crids) : base(crids) { }
        public CatExceptionDatabaseLockTimeout(string errorMessage) : base(errorMessage) { }
        public CatExceptionDatabaseLockTimeout(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionDatabaseLockTimeout(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionDatabaseLockTimeout(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionNoSqlConnection: CatException
{
#pragma warning disable format // @formatter:off
        public CatExceptionNoSqlConnection() : base(Guid.Empty) { }
        public CatExceptionNoSqlConnection(Guid crids) : base(crids) { }
        public CatExceptionNoSqlConnection(string errorMessage) : base(errorMessage) { }
        public CatExceptionNoSqlConnection(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionNoSqlConnection(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionNoSqlConnection(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionDebugFailure: CatException
{
#pragma warning disable format // @formatter:off
        public CatExceptionDebugFailure() : base(Guid.Empty) { }
        public CatExceptionDebugFailure(Guid crids) : base(crids) { }
        public CatExceptionDebugFailure(string errorMessage) : base(errorMessage) { }
        public CatExceptionDebugFailure(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionDebugFailure(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionDebugFailure(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}

public class CatExceptionSchemaUpdateFailed: CatException
{
#pragma warning disable format // @formatter:off
        public CatExceptionSchemaUpdateFailed() : base(Guid.Empty) { }
        public CatExceptionSchemaUpdateFailed(Guid crids) : base(crids) { }
        public CatExceptionSchemaUpdateFailed(string errorMessage) : base(errorMessage) { }
        public CatExceptionSchemaUpdateFailed(string errorMessage, Exception innerException) : base(errorMessage, innerException) { }
        public CatExceptionSchemaUpdateFailed(Guid crids, string errorMessage) : base(crids, errorMessage) { }
        public CatExceptionSchemaUpdateFailed(Guid crids, Exception innerException, string errorMessage) : base(crids, innerException, errorMessage) { }
#pragma warning restore format // @formatter:on
}