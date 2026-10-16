namespace NoteekFlow.Models;

public sealed class DirectoryItem : FileSystemItem
{
    public override bool IsDirectory => true;
    public override string Icon => "▰";
    public DirectoryItem(string path) : base(path) { }
}
