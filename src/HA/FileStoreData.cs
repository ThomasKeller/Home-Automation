namespace HA;

public struct FileStoreData
{
    public int FileCount { get; set; }

    public List<string>? Lines { get; set; }

    public FileInfo? FileInfo { get; set; }
}