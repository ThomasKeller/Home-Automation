using Microsoft.Extensions.Logging;

namespace HA;

public partial class FileStore : IFileStore
{
    private readonly ILogger _logger;

    public string FileExtension { get; set; } = "txt";

    public string BakFileExtension { get; set; } = "bak";

    public string FileNamePrefix { get; set; } = string.Empty;

    public string DirectoryPath { get; set; } = string.Empty;

    public DirectoryInfo? DirectoryInfo { get; private set; }

    public FileStore(ILogger logger, string path = "")
    {
        _logger = logger;
        if (string.IsNullOrEmpty(path))
        {
            path = Directory.GetCurrentDirectory();
        }
        DirectoryPath = path;
        try
        {
            if (!Directory.Exists(DirectoryPath))
            {
                DirectoryInfo = Directory.CreateDirectory(DirectoryPath);
                _logger.LogInformation($"Directory created: {DirectoryInfo.FullName}");
            }
            else
            {
                var fullPath = Path.GetFullPath(DirectoryPath);
                DirectoryInfo = new DirectoryInfo(fullPath);
                _logger.LogInformation($"Use directory: {DirectoryInfo.FullName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ex.Message);
        }
    }

    public void WriteToFile(string line, bool addNewLineIfNeeded = true)
    {
        var todayTicks = DateTime.Today.Ticks;
        var fileName = Path.Combine(
            DirectoryInfo?.FullName ?? ".\\",
            $"{FileNamePrefix}{todayTicks}.{FileExtension}");
        try
        {
            if (addNewLineIfNeeded && !line.EndsWith("\n"))
                line += Environment.NewLine;
            File.AppendAllText(fileName, line);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogCritical(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ex.Message);
        }
    }

    public void WriteToFile(List<string> lines, bool addNewLineIfNeeded = true)
    {
        var todayTicks = DateTime.Today.Ticks;
        var fileName = Path.Combine(
            DirectoryInfo?.FullName ?? ".\\",
            $"{FileNamePrefix}{todayTicks}.{FileExtension}");
        try
        {
            foreach (var line in lines)
            {
                var tempLine = line;
                if (addNewLineIfNeeded && !tempLine.EndsWith("\n"))
                    tempLine += Environment.NewLine;
                File.AppendAllText(fileName, tempLine);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogCritical(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ex.Message);
        }
    }

    public FileStoreData ReadFirstFile()
    {
        var files = Directory.GetFiles(DirectoryPath, $"*.{FileExtension}", new EnumerationOptions());
        if (files.Length > 0)
        {
            var fileInfo = files
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.CreationTime)
                .First();
            var lines = File.ReadAllLines(fileInfo.FullName);
            return new FileStoreData
            {
                FileCount = files.Length,
                FileInfo = fileInfo,
                Lines = new List<string>(lines)
            };
        }
        else
        {
            return new FileStoreData
            {
                FileCount = files.Length
            };
        }
    }

    public bool MarkAsProcessed(FileInfo fileInfo)
    {
        if (fileInfo != null)
        {
            try
            {
                var fileName = fileInfo.FullName + $".{BakFileExtension}";
                if (File.Exists(fileName))
                {
                    _logger.LogInformation($"Append to content to '{fileName}'");
                    var lines = File.ReadAllLines(fileInfo.FullName);
                    File.AppendAllLines(fileName, lines);
                }
                else
                {
                    _logger.LogInformation($"Rename file to '{fileName}'");
                    fileInfo.MoveTo(fileName);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
            }
        }
        return false;
    }

    public void Store(string json)
    {
        WriteToFile(json);
    }
}