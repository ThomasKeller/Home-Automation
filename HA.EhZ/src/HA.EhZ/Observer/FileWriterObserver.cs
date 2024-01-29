using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace HA.EhZ.Observer;

/// <summary>
/// Write the byte stream from the Observable into a file.
/// </summary>
public class FileWriterObserver : IObserver<byte[]>, IDisposable
{
    private readonly ILogger _logger;
    private FileStream _fileStream;

    public long ByteWritten { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="filePath">file name to store the byte stream</param>
    public FileWriterObserver(ILogger logger, string filePath)
    {
        _logger = logger;
        _fileStream = File.OpenWrite(filePath);
    }

    public void OnCompleted()
    {
        _logger.LogInformation("FileRecorder:OnComplete");
        _fileStream.Close();
        _fileStream.Dispose();
        _fileStream = null;
    }

    public void OnError(Exception error)
    {
        _logger.LogError(error, error.Message);
    }

    public void OnNext(byte[] bytes)
    {
        _fileStream.Write(bytes, 0, bytes.Length);
        ByteWritten += bytes.Length;
    }

    public void Dispose()
    {
        _fileStream?.Dispose();
        _fileStream = null;
    }
}