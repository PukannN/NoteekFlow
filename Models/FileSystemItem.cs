using System;
using System.IO;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace NoteekFlow.Models;

public abstract class FileSystemItem
{
    public string Name { get; }
    public string FullPath { get; }
    public virtual bool IsDirectory => false;
    public ObservableCollection<FileSystemItem> Children { get; } = new();
    public abstract Bitmap Icon { get; }

    protected FileSystemItem(string path)
    {
        FullPath = path;
        Name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(Name))
        {
            Name = path;
        }
    }

    protected Bitmap getItemIcon(string iconName)
    {   
        return new Bitmap(AssetLoader.Open(new Uri($"avares://NoteekFlow/Assets/FileIcons/{iconName}")));
    }
}
