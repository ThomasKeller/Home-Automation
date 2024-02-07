using RestSharp;

namespace HA.Influx;


public class BadRequestException : RestApiException
{
    public BadRequestException(RestResponse restResponse)
        : base(restResponse) { }

    public BadRequestException(string message, RestResponse restResponse)
        : base(message, restResponse) { }

    public BadRequestException(string message, Exception innerException, RestResponse restResponse)
        : base(message, innerException, restResponse) { }
}
