using RestSharp;
using System.Net;

namespace HA.Influx;

public partial class InfluxSimpleStore : IInfluxStore, IObserverProcessor
{
    private readonly string _url;
    private readonly string _token;
    private readonly string _bucket;
    private readonly string _org;

    private RestClient _client;

    public int Timeout { get; set; } = 30000;

    public TimeResolution Resolution { get; set; } = TimeResolution.ns;

    public InfluxSimpleStore(string? url, string? bucket, string? org, string? token)
    {
        _url = url ?? throw new ArgumentNullException(nameof(url));
        _token = token ?? throw new ArgumentNullException(nameof(token));
        _org = org ?? throw new ArgumentNullException(nameof(org));
        _bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
        _client = new RestClient(_url);
    }

    public bool Ping()
    {
        var response = _client.Execute(new RestRequest("/ping", Method.Get)
        {
            Timeout = Timeout
        });
        return response.IsSuccessful;
    }

    public bool CheckHealth()
    {
        var request = new RestRequest("/health", Method.Get)
        {
            Timeout = Timeout
        };
        request = AddHeader(request);
        var response = _client.Execute(request);
        return response.IsSuccessful;
    }

    public void WriteMeasurements(IEnumerable<Measurement> measurements)
    {
        // TimeResolution "ms" "s" "us" "ns"
        var resolution = $"{Resolution}";
        var resource = $"/api/v2/write?bucket={_bucket}&org={_org}&precision={resolution}";
        var request = new RestRequest(resource, Method.Post) { Timeout = Timeout };
        request = AddHeader(request);
        var body = string.Empty;
        foreach (var measurment in measurements)
        {
            if (!string.IsNullOrEmpty(body))
                body += "\n";
            var lineProtocol = measurment.ToLineProtocol(Resolution);
            body += lineProtocol;
        }
        request.AddParameter("text/plain", body, ParameterType.RequestBody);
        var response = _client.Execute(request);
        ThrowExceptionIfNeeded(response, measurements.ToArray());
    }

    public void WriteMeasurement(Measurement measurement)
    {
        // TimeResolution "ms" "s" "us" "ns"
        var resolution = $"{Resolution}";
        var resource = $"/api/v2/write?bucket={_bucket}&org={_org}&precision={resolution}";
        var request = new RestRequest(resource, Method.Post) { Timeout = Timeout };
        request = AddHeader(request);
        request.AddParameter("text/plain", measurement.ToLineProtocol(Resolution), ParameterType.RequestBody);
        var response = _client.Execute(request);
        ThrowExceptionIfNeeded(response, measurement);
    }

    public void ProcessMeasurement(Measurement measurement)
    {
        WriteMeasurement(measurement);
    }

    private RestRequest AddHeader(RestRequest request)
    {
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Content-Type", "text/plain");
        request.AddHeader("Authorization", $"Token {_token}");
        //request.AddHeader("Accept-Encoding", "gzip, deflate, br");
        return request;
    }

    private ResponseResult CreateResponseResult(RestResponse response)
    {
        return new ResponseResult
        {
            ErrorMessage = response.ErrorMessage,
            StatusCode = response.StatusCode,
            IsSuccessful = response.IsSuccessful
        };
    }

    private void ThrowExceptionIfNeeded(RestResponse response, Measurement measurement)
    {
        ThrowExceptionIfNeeded(response, new[] { measurement });
    }

    private void ThrowExceptionIfNeeded(RestResponse response, Measurement[] measurements)
    {
        var exception = CreateExceptionIfNeeded(response, measurements);
        if (exception != null)
            throw exception;
    }

    private Exception? CreateExceptionIfNeeded(RestResponse response, Measurement[] measurements)
    {
        if (!response.IsSuccessful)
        {
            var message = response.ErrorMessage ?? response.StatusCode.ToString();
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    return new UnauthorizedException(message, response);

                case HttpStatusCode.BadRequest:
                    return new BadRequestException(message, response);

                default:
                    return new RestApiException(message, response);
            }
        }
        return null;
    }
}