using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NoteekFlow.Models;
public class DirectoryService
{

    public string RootPath {get; private set;} = string.Empty;
    public ObservableCollection<FileItem> Items { get; } = new();

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

    private static List<FileItem> FetchDirectoryItems(string path)
    {
        var items = new List<FileItem>();

        try
        {
            var dirInfo = new DirectoryInfo(path);

            var directories = dirInfo.EnumerateDirectories()
                .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
                .OrderBy(d => d.Name)
                .Select(d =>
                {
                   var folderItem = new FileItem(d.FullName, isDirectory: true);
                   var subItems = FetchDirectoryItems(d.FullName);
                   foreach (var child in subItems)
                    {
                        folderItem.Children.Add(child);
                    } 
                    return folderItem;
                });
            var files = dirInfo.EnumerateFiles()
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                .OrderBy(f => f.Name)                
                .Select(f => new FileItem(f.FullName, isDirectory: false));
            
            items.AddRange(directories);
            items.AddRange(files);
        }
        catch(UnauthorizedAccessException)
        {

        }
        catch (Exception)
        {
            
        }

        return items;
    }

}