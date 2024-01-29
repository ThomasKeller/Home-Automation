using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Sockets;

namespace HA.EhZ.Observer;

/// <summary>
/// Broadcast the byte stream that is comming from the Observable.
/// </summary>
public sealed class UdpServerObserver : IObserver<byte[]>, IDisposable
{
    private readonly ILogger _logger;
    private readonly Socket _socket;
    private readonly EndPoint _endpoint;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="address">Braodcast IP Address: 192.168.111.255</param>
    /// <param name="port">Port e.g. 5557</param>
    public UdpServerObserver(ILogger logger, string address, int port)
    {
        _logger = logger;
        var broadcast = IPAddress.Parse(address);
        _endpoint = new IPEndPoint(broadcast, port);
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    public void OnCompleted()
    {
        _logger.LogInformation("UdpServerObserver::OnCompleted");
    }

    public void OnError(Exception error)
    {
        _logger.LogError(error, error.Message);
    }

    public void OnNext(byte[] data)
    {
        Send(data);
    }

    public void Send(byte[] bytesToSend)
    {
        try
        {
            _logger.LogDebug("Send {0} bytes.", bytesToSend.Length);
            _socket.SendTo(bytesToSend, _endpoint);
        }
        catch (Exception error)
        {
            _logger.LogError(error, error.Message);
            OnError(error);
        }
    }

    public void Dispose()
    {
        _logger.LogDebug("UdpServerObserver::Dispose");
        _socket?.Close();
        _socket?.Dispose();
    }
}