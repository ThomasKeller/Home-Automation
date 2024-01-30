using System;
using RestSharp;

namespace HA.IOBroker;

public class IoBrokerClient
{
    private RestClient m_RestClient;

    public Uri BaseUrl { get; set; }

    public IoBrokerClient(string baseUrl)
    {
        BaseUrl = new Uri(baseUrl);
    }

    public void Test()
    {
        if (m_RestClient == null)
        {
            m_RestClient = new RestClient(BaseUrl);
        }
        //client.Authenticator = new HttpBasicAuthenticator("username", "password");
        //var client = new RestClient("https://api.twitter.com/1.1");
        //var request = new RestRequest("statuses/home_timeline.json", Method.GET);
        //var response = client.Get(request);
    }

    /*public List<string> Search(string pattern = "*")
    {
        Init();
        var relativeUrl = $"search?pattern={pattern}";
        var request = new RestRequest(relativeUrl, Method.Get);
        var response = m_RestClient.Execute(request);
        return SimpleJson.DeserializeObject<List<string>>(response.Content);
    }*/

    private void Init()
    {
        if (m_RestClient == null)
        {
            m_RestClient = new RestClient(BaseUrl);
        }
    }
}