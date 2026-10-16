using System.Collections.ObjectModel;
using System.IO;

namespace NoteekFlow.Models;

public abstract class FileSystemItem
{
    public string Name { get; }
    public string FullPath { get; }
    public virtual bool IsDirectory => false;
    public ObservableCollection<FileSystemItem> Children { get; } = new();
    public abstract string Icon { get; }

    protected FileSystemItem(string path)
    {
        FullPath = path;
        Name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(Name))
        {
            Name = path;
        }
    }
}
