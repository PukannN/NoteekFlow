using System.IO;

namespace NoteekFlow.Models;

public sealed class FileItem : FileSystemItem
{
    public FileItemType Type => Path.GetExtension(FullPath).ToLowerInvariant() switch
    {
        ".noteek" => FileItemType.NOTEEK,
        ".md" => FileItemType.MARKDOWN,
        ".png" or ".jpeg" or ".jpg" or ".gif" or ".bmp" or ".webp" => FileItemType.IMAGE,
        ".pdf" => FileItemType.PDF,
        ".txt" => FileItemType.TEXT,
        _ => FileItemType.UNKNOWN
    };

    public override string Icon => Type switch
    {
        FileItemType.MARKDOWN => "M",
        FileItemType.NOTEEK => "N",
        FileItemType.IMAGE => "▧",
        FileItemType.PDF => "P",
        FileItemType.TEXT => "T",
        _ => "·"
    };

    public FileItem(string path) : base(path) { }
}
