namespace HA.Redis;

public interface IRedisPushToStreamClient
{
    bool PushToStream(Measurement measurement);
}