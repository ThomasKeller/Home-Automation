using System;
using System.IO.Ports;
using HA.Observable;
using Microsoft.Extensions.Logging;

namespace HA.EhZ.Observable;

/// <summary>
/// Open the Serial Port and listen for incomming data from the EhZ.
/// The incomming data will be reported to the Observers (OnNext).
/// </summary>
public class SerialPortObservable : ObservableBase<byte[]>
{
    private readonly ILogger _logger;
    private readonly SerialPort _serialPort;

    public DateTime LastBytesSentAt { get; private set; } = DateTime.MinValue;

    public long LastBytesSentCount { get; private set; } = 0;

    public long BytesSentCount { get; set; } = 0;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="comPort">Serial COM port name</param>
    public SerialPortObservable(ILogger logger, string comPort)
    {
        _logger = logger;
        if (comPort == null) throw new ArgumentNullException(nameof(comPort));
        _serialPort = new SerialPort(comPort);
        InitializeSerialPort();
    }

    public void Stop()
    {
        _logger.LogInformation("stop reading from serial port. Inform observers");
        ExecuteOnComplete();
        CloseSerialPort();
    }

    private void InitializeSerialPort()
    {
        try
        {
            _logger.LogInformation("Open serial port: {comPort}", _serialPort.PortName);
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            _serialPort.DataBits = 8;
            _serialPort.Handshake = Handshake.None;
            _serialPort.DataReceived += DataReceivedHandler;
            _serialPort.Open();
            _serialPort.RtsEnable = true;
            _serialPort.DtrEnable = true;
            _logger.LogInformation("Port Status {0}", _serialPort.IsOpen);
            _serialPort.RtsEnable = true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal Error: Close Serial Port: {errorMsg}", ex.Message);
            CloseSerialPort();
            throw;
        }
    }

    private void CloseSerialPort()
    {
        _serialPort.DataReceived -= DataReceivedHandler;
        _serialPort.Close();
        _serialPort.Dispose();
    }

    private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var serialPort = (SerialPort)sender;
            var bytesToRead = serialPort.BytesToRead;
            if (bytesToRead > 0)
            {
                var buffer = new byte[bytesToRead];
                var readBytes = serialPort.Read(buffer, 0, bytesToRead);
                LastBytesSentAt = DateTime.Now;
                LastBytesSentCount = buffer.Length;
                BytesSentCount += LastBytesSentCount;
                ExecuteOnNext(buffer);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            _serialPort.BaseStream.Flush();
        }
    }
}