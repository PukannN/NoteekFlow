using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform.Storage;

class DirectoryContent
{

    public string DirectoryPath {get;set;} = String.Empty;
    public ObservableCollection<string> DirectoryItems { get; } = new();

    //TODO: Make a file class to store each files extension, name, path etc.
    public static ObservableCollection<string> GetDirectoryItems(string directoryPath)
    {
        
        try
        {
            if (!Directory.Exists(directoryPath)) return new();
            else
            { 
                return Directory.EnumerateFileSystemEntries(directoryPath)
                                .Select(Path.GetFileName)
                                .OfType<string>()
                                .ToObservableCollection();
            }
        }
        catch
        {
            return new();        
        }


    
    }

}