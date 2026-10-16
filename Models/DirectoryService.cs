using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NoteekFlow.Models;
public class DirectoryService
{

    public string RootPath {get; private set;} = string.Empty;
    public ObservableCollection<FileSystemItem> Items { get; } = new();

    public void LoadDirectory(string directoryPath)
    {
        Items.Clear();
        RootPath = directoryPath;

        if(string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath)) return;

        var rootItems = FetchDirectoryItems(directoryPath);
        foreach(var item in rootItems)
        {
            Items.Add(item);
        }
    } 

    private static List<FileSystemItem> FetchDirectoryItems(string path)
    {
        var items = new List<FileSystemItem>();

        var dirInfo = new DirectoryInfo(path);

        var directories = dirInfo.EnumerateDirectories()
            .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
            .OrderBy(d => d.Name)
            .Select(d => LoadDirectoryItem(d));
        var files = dirInfo.EnumerateFiles()
            .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
            .OrderBy(f => f.Name)
            .Select(f => new FileItem(f.FullName));

        items.AddRange(directories);
        items.AddRange(files);

        return items;
    }

    private static DirectoryItem LoadDirectoryItem(DirectoryInfo directory)
    {
        var item = new DirectoryItem(directory.FullName);
        if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            return item;
        }

        foreach (var child in FetchDirectoryItems(directory.FullName))
        {
            item.Children.Add(child);
        }

        return item;
    }
}