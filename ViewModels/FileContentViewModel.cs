using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteekFlow.Models;
using NoteekFlow.Resources;

namespace NoteekFlow.ViewModels;

public partial class FileContentViewModel : ViewModelBase
{
    [ObservableProperty]
    private FileItem? _fileItem;

    [ObservableProperty]
    private string _title = Strings.WelcomeTitle;

    [ObservableProperty]
    private bool _isDirty;

    public event EventHandler? CloseRequested;

    protected FileContentViewModel(FileItem? fileItem = null)
    {
        FileItem = fileItem;
        if (fileItem is not null)
        {
            Title = fileItem.Name;
        }
    }

    [RelayCommand]
    private void CloseTab() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
