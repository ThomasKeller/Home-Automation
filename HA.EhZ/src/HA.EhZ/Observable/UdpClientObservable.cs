using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HA.Observable;

namespace HA.EhZ.Observable;

public sealed class UdpClientObservable : ObservableBase<byte[]>
{
    private readonly ILogger _logger;
    private readonly CancellationTokenSource m_TokenSource = new();
    private readonly ManualResetEvent m_ManualResetEvent = new(false);
    private readonly int _port;
    private Task m_Task;

    public UdpClientObservable(ILogger logger, int port)
    {
        _logger = logger;
        _port = port;
    }

    public void Start()
    {
        _logger.LogInformation("start UDP client on port: {port}", _port);
        if (m_Task == null)
        {
            m_Task = new Task(() => ReadFromClient(), m_TokenSource.Token);
            m_Task.Start();
        }
    }

    public void Stop()
    {
        _logger.LogInformation("stop UDP client on port: {port}", _port);
        m_TokenSource.Cancel();
        m_ManualResetEvent.WaitOne(1000);
        ExecuteOnComplete();
    }

    private void ReadFromClient()
    {
        var listener = new UdpClient(new IPEndPoint(IPAddress.Any, 5557));// "0.0.0.0", _port);
        listener.EnableBroadcast = true;
        IPEndPoint groupEP = new IPEndPoint(0, 0);
        //listener.Client.Bind(new IPEndPoint(IPAddress.Any, 5557));
        try
        {
            while (!m_TokenSource.Token.IsCancellationRequested)
            {
                byte[] bytes = listener.Receive(ref groupEP);
                ExecuteOnNext(bytes);
            }
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, ex.Message);
            ExecuteOnError(ex);
        }
        finally
        {
            listener.Close();
        }
    }

    private static UdpClient ConnectUDPClient()
    {
        var port = 5557;
        var interfaceIp = "0.0.0.0";
        var udpClient = new UdpClient(interfaceIp, port);
        return udpClient;
    }
}