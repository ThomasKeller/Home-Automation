namespace HA.Kostal;

public interface IKostalClient
{
    Task<KostalClientResult> readPageAsync();
}