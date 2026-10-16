using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteekFlow.Models;
using NoteekFlow.Resources;
using NoteekFlow.Views;

namespace NoteekFlow.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<FileContentViewModel> _openTabs = new();

    [ObservableProperty]
    private FileContentViewModel? _currentEditorViewModel;

    [ObservableProperty]
    private string _statusText = Strings.Ready;

    public FileExplorerViewModel FileExplorer { get; }
    public NavigationBarViewModel NavigationBar { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel(IStorageProvider? storageProvider = null)
    {
        var preferencesStore = new UserPreferencesStore();
        UserPreferences preferences;
        string? preferencesError = null;
        try
        {
            preferences = preferencesStore.Load();
        }
        catch (Exception ex) when (UserPreferencesStore.IsRecoverableLoadError(ex))
        {
            preferences = new UserPreferences();
            preferencesError = Strings.Format("LoadSettingsError", ex.Message);
        }

        Settings = new SettingsViewModel(preferencesStore, preferences);
        FileExplorer = new FileExplorerViewModel(
            OpenFileAsync,
            (title, initialText) => WorkspaceDialogService.PromptForTextAsync(title, initialText),
            WorkspaceDialogService.ConfirmAsync,
            CanModifyItem);
        FileExplorer.ItemRenamed += OnItemRenamed;
        FileExplorer.ItemDeleted += OnItemDeleted;
        NavigationBar = new NavigationBarViewModel(storageProvider, Settings);
        NavigationBar.WorkspaceSelected += OpenWorkspace;
        NavigationBar.FileCreated += OnFileCreated;
        Settings.ApplyTheme();
        CurrentEditorViewModel = new DefaultWorkspaceViewModel();

        if (!string.IsNullOrWhiteSpace(preferencesError))
        {
            StatusText = preferencesError;
        }
        else if (!string.IsNullOrWhiteSpace(preferences.LastWorkspacePath)
            && Directory.Exists(preferences.LastWorkspacePath))
        {
            OpenWorkspace(preferences.LastWorkspacePath);
        }
    }

    partial void OnCurrentEditorViewModelChanged(FileContentViewModel? value)
    {
        StatusText = value?.FileItem?.FullPath ?? Strings.Ready;
    }

    private void OpenWorkspace(string directoryPath)
    {
        FileExplorer.UpdateNavDirectory(directoryPath);
        if (FileExplorer.ErrorMessage is null)
        {
            NavigationBar.WorkspaceName = Path.GetFileName(
                directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            Settings.SetLastWorkspace(directoryPath);
        }

        StatusText = Settings.ErrorMessage
            ?? FileExplorer.ErrorMessage
            ?? Strings.Format("WorkspaceStatus", directoryPath);
    }

    private void OnFileCreated(string path)
    {
        var directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            NavigationBar.WorkspaceName = Path.GetFileName(
                directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            FileExplorer.UpdateNavDirectory(directoryPath);
            Settings.SetLastWorkspace(directoryPath);
        }

        _ = OpenFileAsync(new FileItem(path));
    }

    private bool CanModifyItem(FileSystemItem item)
    {
        if (OpenTabs.Any(tab =>
                tab.IsDirty
                && tab.FileItem is { } file
                && IsPathWithin(file.FullPath, item.FullPath)))
        {
            StatusText = Strings.CannotDeleteDirtyFile;
            return false;
        }

        return true;
    }

    private void OnItemDeleted(string deletedPath) =>
        RemoveTabsForPath(deletedPath);

    private void OnItemRenamed(string oldPath, string newPath)
    {
        var currentTab = CurrentEditorViewModel;
        var renamedTabs = OpenTabs
            .Where(tab => tab.FileItem is { } file && IsPathWithin(file.FullPath, oldPath))
            .Select(tab => (Tab: tab, OldFilePath: tab.FileItem!.FullPath))
            .ToArray();
        foreach (var (tab, _) in renamedTabs)
        {
            RemoveTab(tab);
        }

        var currentTabWasRenamed = renamedTabs.Any(item => ReferenceEquals(currentTab, item.Tab));
        if (currentTabWasRenamed)
        {
            CurrentEditorViewModel = OpenTabs.Count == 0
                ? new DefaultWorkspaceViewModel()
                : OpenTabs[^1];
        }

        _ = ReopenRenamedTabsAsync(oldPath, newPath, renamedTabs, currentTabWasRenamed ? null : currentTab);
    }

    private async Task ReopenRenamedTabsAsync(
        string oldPath,
        string newPath,
        IReadOnlyCollection<(FileContentViewModel Tab, string OldFilePath)> tabs,
        FileContentViewModel? tabToRestore)
    {
        foreach (var (_, oldFilePath) in tabs)
        {
            var relativePath = Path.GetRelativePath(oldPath, oldFilePath);
            var newFilePath = string.Equals(relativePath, ".", StringComparison.Ordinal)
                ? newPath
                : Path.Combine(newPath, relativePath);
            if (File.Exists(newFilePath))
            {
                await OpenFileAsync(new FileItem(newFilePath));
            }
        }

        if (tabToRestore is not null && OpenTabs.Contains(tabToRestore))
        {
            CurrentEditorViewModel = tabToRestore;
        }
    }

    private void RemoveTabsForPath(string path)
    {
        var removedTabs = OpenTabs
            .Where(tab => tab.FileItem is { } file && IsPathWithin(file.FullPath, path))
            .ToArray();
        var removedCurrent = removedTabs.Any(tab => ReferenceEquals(CurrentEditorViewModel, tab));
        foreach (var tab in removedTabs)
        {
            RemoveTab(tab);
        }

        if (removedCurrent)
        {
            CurrentEditorViewModel = OpenTabs.Count == 0
                ? new DefaultWorkspaceViewModel()
                : OpenTabs[^1];
        }
    }

    private void RemoveTab(FileContentViewModel tab)
    {
        OpenTabs.Remove(tab);
        if (tab is ImageViewModel imageTab)
        {
            imageTab.DisposeImage();
        }
        else if (tab is PdfViewModel pdfTab)
        {
            pdfTab.DisposePages();
        }
    }

    private static bool IsPathWithin(string path, string parentPath)
    {
        var relativePath = Path.GetRelativePath(parentPath, path);
        return !Path.IsPathRooted(relativePath)
            && relativePath != ".."
            && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private async Task OpenFileAsync(FileItem file)
    {
        var existingTab = OpenTabs.FirstOrDefault(tab =>
            string.Equals(
                tab.FileItem?.FullPath,
                file.FullPath,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
        if (existingTab is not null)
        {
            CurrentEditorViewModel = existingTab;
            return;
        }

        FileContentViewModel? viewModel = null;
        try
        {
            viewModel = file.Type switch
            {
                FileItemType.NOTEEK => new NoteekViewModel(file),
                FileItemType.MARKDOWN => new MarkdownViewModel(file),
                FileItemType.TEXT => new TextFileViewModel(file),
                FileItemType.IMAGE => new ImageViewModel(file),
                FileItemType.PDF => new PdfViewModel(file),
                _ => new DefaultWorkspaceViewModel(file)
            };

            viewModel.CloseRequested += OnTabCloseRequested;
            OpenTabs.Add(viewModel);
            CurrentEditorViewModel = viewModel;
            StatusText = file.FullPath;

            if (viewModel is TextFileViewModel textViewModel)
            {
                await textViewModel.LoadTextAsync();
                if (ReferenceEquals(CurrentEditorViewModel, viewModel))
                {
                    StatusText = textViewModel.ErrorMessage ?? file.FullPath;
                }
            }
            else if (viewModel is NoteekViewModel noteekViewModel)
            {
                await noteekViewModel.LoadAsync();
            }
            else if (viewModel is PdfViewModel pdfViewModel)
            {
                await pdfViewModel.LoadAsync();
            }
            else if (viewModel is ImageViewModel imageViewModel)
            {
                await imageViewModel.LoadImageAsync(file.FullPath);
                if (ReferenceEquals(CurrentEditorViewModel, viewModel))
                {
                    StatusText = imageViewModel.ErrorMessage ?? file.FullPath;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to open file '{file.FullPath}': {ex}");
            if (viewModel is not null)
            {
                OpenTabs.Remove(viewModel);
                if (ReferenceEquals(CurrentEditorViewModel, viewModel))
                {
                    CurrentEditorViewModel = OpenTabs.Count == 0
                        ? new DefaultWorkspaceViewModel()
                        : OpenTabs[^1];
                }

                if (viewModel is ImageViewModel imageViewModel)
                {
                    imageViewModel.DisposeImage();
                }
                else if (viewModel is PdfViewModel pdfViewModel)
                {
                    pdfViewModel.DisposePages();
                }
            }

            StatusText = Strings.Format("OpenFileError", file.Name, ex.Message);
        }
    }

    private void OnTabCloseRequested(object? sender, EventArgs e)
    {
        if (sender is not FileContentViewModel tab)
        {
            return;
        }

        if (tab.IsDirty)
        {
            StatusText = Strings.SaveBeforeClose;
            return;
        }

        var index = OpenTabs.IndexOf(tab);
        RemoveTab(tab);

        if (ReferenceEquals(CurrentEditorViewModel, tab))
        {
            CurrentEditorViewModel = OpenTabs.Count == 0
                ? new DefaultWorkspaceViewModel()
                : OpenTabs[Math.Clamp(index - 1, 0, OpenTabs.Count - 1)];
        }
    }

    [RelayCommand]
    private async Task SaveCurrentFileAsync()
    {
        if (CurrentEditorViewModel is NoteekViewModel noteekViewModel)
        {
            await noteekViewModel.SaveAsync();
            StatusText = noteekViewModel.ErrorMessage ?? (noteekViewModel.IsDirty ? Strings.SaveFailed : Strings.Saved);
            return;
        }

        if (CurrentEditorViewModel is not TextFileViewModel textViewModel)
        {
            StatusText = Strings.CannotSaveActiveFile;
            return;
        }

        await textViewModel.SaveAsync();
        StatusText = textViewModel.ErrorMessage ?? (textViewModel.IsDirty ? Strings.SaveFailed : Strings.Saved);
    }

    public async Task<bool> SaveDirtyNoteekFilesAsync()
    {
        foreach (var note in OpenTabs.OfType<NoteekViewModel>().ToArray())
        {
            await note.SaveAsync();
            if (note.IsDirty)
            {
                StatusText = note.ErrorMessage ?? Strings.SaveFailed;
                return false;
            }
        }

        return true;
    }
}
