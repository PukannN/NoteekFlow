using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NoteekFlow.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IStorageProvider? _storageProvider;

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty]
    private bool _isPaneOpen = true;
    
    [ObservableProperty]
    private ObservableCollection<string> _navItems = DirectoryContent.GetDirectoryItems("c:\\Users\\matej\\OneDrive");

    [ObservableProperty]
    private string _directoryPath = string.Empty; //A proper file path from the file explorer shall go here

    public MainViewModel()
    {
        
    }
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
            Title = "Select a Folder to Open",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var folder = folders[0];
            DirectoryPath = folder.TryGetLocalPath() ?? folder.Name;
            NavItems = DirectoryContent.GetDirectoryItems(DirectoryPath);
            
        }
    }
    

}
