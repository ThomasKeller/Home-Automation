using RestSharp;

namespace HA.Influx;

public class RestApiException : Exception
{
    public RestResponse Response { get; protected set; }

    public RestApiException(RestResponse restResponse)
    { Response = restResponse; }

    public RestApiException(string message, RestResponse restResponse)
        : base(message) { Response = restResponse; }

    public RestApiException(string message, Exception innerException, RestResponse restResponse)
        : base(message, innerException) { Response = restResponse; }
}
