using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using NoteekFlow.Models;

namespace NoteekFlow.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IStorageProvider? _storageProvider;
    private readonly DirectoryService _directoryService = new();

    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private ObservableCollection<FileItem> _navFiles = new();
    
    [ObservableProperty]
    private FileItem? _selectedFile;

    partial void OnSelectedFileChanged(FileItem? value)
    {
        if (value != null && !value.IsDirectory)
        {
            OpenFile(value);
        }
    }

    [ObservableProperty]
    private string _directoryPath = string.Empty;

    public MainViewModel(){}

    public MainViewModel(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    [RelayCommand]
    private void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }    
    
    [RelayCommand]
    private async Task OpenDirectoryAsync()
    {
        if (_storageProvider == null) return;

        
        var folders = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select a Workspaces Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var folder = folders[0];
            DirectoryPath = folder.TryGetLocalPath() ?? folder.Name;

            _directoryService.LoadDirectory(DirectoryPath);
            NavFiles = _directoryService.Items;            
        }    
    }

    [RelayCommand]
    private async Task CreateFileAsync(string defaultContent)
    {

        if (_storageProvider == null) return;

        var customFileType = new FilePickerFileType("Noteek Canvas")
        {
            Patterns = new[] {"*.noteek", "*.md"}
        };

        var newFile = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{
            Title = "Create New File",
            DefaultExtension = ".md",
            FileTypeChoices = new[] {customFileType}
        });

        if (newFile == null) 
        {
            Debug.Print("File creation canceled");
            return;
        } 
        else Debug.Print("File succesfully created");
        
        var localPath = newFile.TryGetLocalPath();
        if (!string.IsNullOrEmpty(localPath))
        {
            if (localPath.EndsWith(".noteek", StringComparison.OrdinalIgnoreCase))
            {
                var initialJson = "{\n \"version\",\n \"elements\": []\n}";
                await File.WriteAllTextAsync(localPath, initialJson);
            }
            else
            {
                await File.WriteAllTextAsync(localPath, "# New Note");
            }

            if (!string.IsNullOrEmpty(DirectoryPath))
            {
                _directoryService.LoadDirectory(DirectoryPath);
                NavFiles = _directoryService.Items;
            }
        }
        
    }

    private async Task OpenFile(FileItem file)
    {
        Debug.Print($"Opening file: {file.FullPath} (Type: {file.Type})");

        switch (file.Type)
        {
            case FileItemType.NoteekCanvas:
                //load Canvas ViewModel
                //CurrentEditorViewModel = new CanvasEditorViewModel(file.fullPath);
                break;

            case FileItemType.Markdown:
                //CurrentEditorViewModel = new TextEditorViewModel(file.fullPath);
                break;

            case FileItemType.Image:
                //CurrentEditorViewModel = new ImageEditorVideoModel(file.fullPath);
                break;

            default:
                Debug.Print("Unssuported file type");
                //add some kind of popup warning or just show the error in CurrentEditViewModel
                break;
               
        }
    }
}
