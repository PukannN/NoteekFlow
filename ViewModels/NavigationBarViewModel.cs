using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteekFlow.Models;
using NoteekFlow.Resources;

namespace NoteekFlow.ViewModels;

public partial class NavigationBarViewModel : ViewModelBase
{
    private readonly IStorageProvider? _storageProvider;

    [ObservableProperty]
    private bool _isExplorerOpen = true;

    [ObservableProperty]
    private Avalonia.Controls.GridLength _explorerColumnWidth = new(280);

    [ObservableProperty]
    private string _workspaceName = Strings.NoWorkspace;

    [ObservableProperty]
    private string? _errorMessage;

    public event Action<string>? WorkspaceSelected;
    public event Action<string>? FileCreated;

    public SettingsViewModel Settings { get; }

    public NavigationBarViewModel(IStorageProvider? storageProvider, SettingsViewModel settings)
    {
        _storageProvider = storageProvider;
        Settings = settings;
        ExplorerColumnWidth = new GridLength(Settings.ExplorerWidth);
        Settings.ExplorerWidthChanged += width =>
        {
            IsExplorerOpen = true;
            ExplorerColumnWidth = new GridLength(width);
        };
    }

    partial void OnIsExplorerOpenChanged(bool value)
    {
        ExplorerColumnWidth = value ? new GridLength(Settings.ExplorerWidth) : new GridLength(0);
        OnPropertyChanged(nameof(ExplorerMinimumWidth));
    }

    public double ExplorerMinimumWidth => IsExplorerOpen ? 200 : 0;

    [RelayCommand]
    private void ToggleExplorer() => IsExplorerOpen = !IsExplorerOpen;

    [RelayCommand]
    private async Task OpenWorkspaceAsync()
    {
        if (_storageProvider is null)
        {
            ErrorMessage = Strings.FolderAccessUnavailable;
            return;
        }

        IReadOnlyList<IStorageFolder> folders;
        try
        {
            folders = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = Strings.WorkspacePickerTitle,
                AllowMultiple = false
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            ErrorMessage = Strings.Format("OpenFolderDialogError", ex.Message);
            return;
        }

        if (folders.Count == 0)
        {
            return;
        }

        var path = folders[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            ErrorMessage = Strings.LocalWorkspaceRequired;
            return;
        }

        WorkspaceName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        ErrorMessage = null;
        WorkspaceSelected?.Invoke(path);
    }

    [RelayCommand]
    private async Task CreateNoteAsync()
    {
        if (_storageProvider is null)
        {
            ErrorMessage = Strings.FileAccessUnavailable;
            return;
        }

        var fileType = new FilePickerFileType(Strings.NoteFileType)
        {
            Patterns = new[] { "*.md", "*.noteek" }
        };

        IStorageFile? file;
        try
        {
            file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Strings.CreateNoteDialogTitle,
                SuggestedFileName = Strings.NewNoteFileName,
                DefaultExtension = ".md",
                FileTypeChoices = new[] { fileType }
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            ErrorMessage = Strings.Format("SaveDialogError", ex.Message);
            return;
        }

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var initialContent = path.EndsWith(".noteek", StringComparison.OrdinalIgnoreCase)
                ? "{\n  \"version\": 1,\n  \"elements\": []\n}"
                : Strings.NewMarkdownContent;
            await File.WriteAllTextAsync(path, initialContent);
            ErrorMessage = null;
            FileCreated?.Invoke(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            Debug.WriteLine($"Unable to create note '{path}': {ex.Message}");
            ErrorMessage = Strings.Format("CreateNoteError", ex.Message);
        }
    }
}
