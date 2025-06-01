namespace AuthChannel.Models;

public class FileInfo
{

    public int Id { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? ContentType { get; set; }

    public string? SequenceId { get; set; }
    public FileInfo() { }
    public FileInfo(int id, string? fileName, long fileSize, string? contentType, string? sequenceId)
    {
        Id = id;
        FileName = fileName;
        FileSize = fileSize;
        ContentType = contentType;
        SequenceId = sequenceId;
    }

}
