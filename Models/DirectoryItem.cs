using Avalonia.Media.Imaging;
namespace NoteekFlow.Models;

public sealed class DirectoryItem : FileSystemItem
{
    public override bool IsDirectory => true;
    public override Bitmap Icon => getItemIcon("folderIcon.png");
    public DirectoryItem(string path) : base(path) { }
}
