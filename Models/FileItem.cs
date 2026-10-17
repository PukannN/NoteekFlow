using System.IO;
using Avalonia.Media.Imaging;


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

    public override Bitmap Icon => Type switch
    {
        FileItemType.MARKDOWN => getItemIcon("mdIcon.png"),
        FileItemType.NOTEEK => getItemIcon("unknownIcon.png"),
        FileItemType.IMAGE => getItemIcon("imageIcon.png"),
        FileItemType.PDF => getItemIcon("pdfIcon.png"),
        FileItemType.TEXT => getItemIcon("txtIcon.png"),
        _ => getItemIcon("unknownIcon.png"),
    };

    public FileItem(string path) : base(path) { }
}
