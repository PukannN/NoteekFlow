using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteekFlow.Models;
using NoteekFlow.Resources;

namespace NoteekFlow.ViewModels;

public partial class TextFileViewModel : ZoomableFileContentViewModel
{
    private bool _isLoading;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public TextFileViewModel(FileItem fileItem) : base(fileItem) { }

    public double EditorFontSize => 14 * Zoom;

    protected override void OnZoomLevelChanged()
    {
        base.OnZoomLevelChanged();
        OnPropertyChanged(nameof(EditorFontSize));
    }

    partial void OnContentChanged(string value)
    {
        if (!_isLoading)
        {
            IsDirty = true;
            ErrorMessage = null;
        }
    }

    public async Task LoadTextAsync()
    {
        if (FileItem is null)
        {
            return;
        }

        try
        {
            _isLoading = true;
            Content = await File.ReadAllTextAsync(FileItem.FullPath);
            IsDirty = false;
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            ErrorMessage = Strings.Format("ReadFileError", ex.Message);
        }
        finally
        {
            _isLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (FileItem is null)
        {
            return;
        }

        try
        {
            await File.WriteAllTextAsync(FileItem.FullPath, Content);
            IsDirty = false;
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            ErrorMessage = Strings.Format("SaveFileError", ex.Message);
        }
    }
}
