using System;
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
