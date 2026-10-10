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

    [ObservableProperty] private FileContentViewModel? _currentEditorViewModel;
    [ObservableProperty] private bool _isPaneOpen = true;
    [ObservableProperty] private string _directoryPath = string.Empty;
    
    public FileExplorerViewModel FileExplorer { get; }

    public MainViewModel()
    {
        FileExplorer = new FileExplorerViewModel(vm => CurrentEditorViewModel = vm);

        CurrentEditorViewModel = new DefaultWorkspaceViewModel();
    }

    public MainViewModel(IStorageProvider storageProvider) : this()
    {
        _storageProvider = storageProvider;
    }

    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;

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
            DirectoryPath = folders[0].TryGetLocalPath() ?? folders[0].Name;
            
            // Delegate the loading to the explorer child component
            FileExplorer.UpdateNavDirectory(DirectoryPath);
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
                FileExplorer.UpdateNavDirectory(DirectoryPath);
            }
        }
        
    }

}
