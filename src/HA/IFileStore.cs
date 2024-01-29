namespace HA;

public interface IFileStore
{
    void WriteToFile(string line, bool addNewLineIfNeeded = true);

    void WriteToFile(List<string> lines, bool addNewLineIfNeeded = true);

    FileStoreData ReadFirstFile();

    bool MarkAsProcessed(FileInfo fileInfo);
}