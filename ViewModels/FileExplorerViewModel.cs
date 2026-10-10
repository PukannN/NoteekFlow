using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using NoteekFlow.Models;
using Avalonia.Media.Imaging;
using Avalonia.Controls;

namespace NoteekFlow.ViewModels;

public partial class FileExplorerViewModel : ViewModelBase
{
    private readonly DirectoryService _directoryService = new();
    private readonly Action<FileContentViewModel?> _onEditorChanged;

    [ObservableProperty]
    private ObservableCollection<FileItem> _navFiles = new();
    
    [ObservableProperty]
    private FileItem? _selectedFile;

    partial void OnSelectedFileChanged(FileItem? value)
    {
        if (value != null)
        {
            OpenFile(value);
        }
    }

    public FileExplorerViewModel(Action<FileContentViewModel?> onEditorChanged)
    {
        _onEditorChanged = onEditorChanged;
    }

    public void UpdateNavDirectory(string directoryPath)
    {
        _directoryService.LoadDirectory(directoryPath);
        NavFiles = _directoryService.Items;
    }

    private void OpenFile(FileItem file)
    {
        if (!File.Exists(file.FullPath))
        {
            Debug.Print($"File missing: {file.FullPath}");
            return;
        }

        switch (file.Type)
        {
            case FileItemType.NoteekCanvas:
                Debug.Print("Loading Canvas format");
    
                break;

            case FileItemType.Markdown:
                Debug.Print("Loading Markdown format");
                // _onEditorChanged(new TextEditorViewModel
                // {
                //     FilePath = file.FullPath,
                //     TextContent = File.ReadAllText(file.FullPath)
                // });
                break;

             case FileItemType.Text:
                Debug.Print("Loading Text format");

                break;


            case FileItemType.Image:
                Debug.Print("Loading Image format");
                var imageViewModel = new ImageVisualizerViewModel();
                _ = imageViewModel.LoadImageAsync(file.FullPath);
                _onEditorChanged(imageViewModel);
                break;
            
            case FileItemType.Folder:
                Debug.Print("Loading Folder format");

                break;

            case FileItemType.Unknown:
                Debug.Print("Unknown file format");
                _onEditorChanged(new DefaultWorkspaceViewModel
                {
                    
                });
                break;
        }
    }
}
