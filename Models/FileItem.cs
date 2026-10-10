using System;
using System.Collections.ObjectModel;
using System.IO;


namespace NoteekFlow.Models;

public class FileItem
{
    public string Name {get; private set;} = string.Empty;
    public string FullPath {get; private set;} = string.Empty;
    public bool IsDirectory {get; private set;}
    public ObservableCollection<FileItem> Children = new();

    public FileItemType Type => IsDirectory
        ? FileItemType.Folder
        : Path.GetExtension(FullPath).ToLowerInvariant() switch
        {
            ".md" => FileItemType.Markdown,
            ".noteek" => FileItemType.NoteekCanvas,
            ".png" or ".jpeg" or ".jpg" => FileItemType.Image,
            ".txt" => FileItemType.Text,
            _ => FileItemType.Unknown
        };

    public FileItem(){}

    public FileItem(string path, bool isDirectory)
    {
        FullPath = path;
        IsDirectory = isDirectory;
        Name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(Name))
        {
            Name = path;
        }
        
    }

}