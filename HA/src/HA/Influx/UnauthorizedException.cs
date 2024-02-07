using RestSharp;

namespace HA.Influx;

public class UnauthorizedException : RestApiException
{
    public UnauthorizedException(RestResponse restResponse)
        : base(restResponse) { }

    public UnauthorizedException(string message, RestResponse restResponse) : base(message, restResponse)
    {
    }

    public UnauthorizedException(string message, Exception innerException, RestResponse restResponse)
        : base(message, innerException, restResponse) { }
}
