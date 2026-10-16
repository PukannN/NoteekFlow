using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteekFlow.Models;
using NoteekFlow.Resources;

namespace NoteekFlow.ViewModels;

public partial class FileExplorerViewModel : ViewModelBase
{
    private readonly DirectoryService _directoryService = new();
    private readonly Func<FileItem, Task> _openFile;
    private readonly Func<string, string?, Task<string?>> _promptForText;
    private readonly Func<string, Task<bool>> _confirm;
    private readonly Func<FileSystemItem, bool> _canModifyItem;
    private bool _suppressOpenOnSelection;

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _navFiles = new();

    [ObservableProperty]
    private FileSystemItem? _selectedItem;

    public bool HasSelectedItem => SelectedItem is not null;

    [ObservableProperty]
    private string? _errorMessage;

    public event Action<string, string>? ItemRenamed;
    public event Action<string>? ItemDeleted;

    public FileExplorerViewModel(
        Func<FileItem, Task> openFile,
        Func<string, string?, Task<string?>> promptForText,
        Func<string, Task<bool>> confirm,
        Func<FileSystemItem, bool> canModifyItem)
    {
        _openFile = openFile;
        _promptForText = promptForText;
        _confirm = confirm;
        _canModifyItem = canModifyItem;
    }

    async partial void OnSelectedItemChanged(FileSystemItem? value)
    {
        OnPropertyChanged(nameof(HasSelectedItem));
        RenameSelectedCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();

        if (_suppressOpenOnSelection)
        {
            _suppressOpenOnSelection = false;
            return;
        }

        if (value is FileItem file)
        {
            await _openFile(file);
        }
    }

    public void SelectContextMenuItem(FileSystemItem? item)
    {
        _suppressOpenOnSelection = true;
        SelectedItem = item;
        _suppressOpenOnSelection = false;
    }

    public void UpdateNavDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            ErrorMessage = Strings.Format("MissingWorkspace", directoryPath);
            SelectedItem = null;
            NavFiles.Clear();
            return;
        }

        try
        {
            SelectedItem = null;
            _directoryService.LoadDirectory(directoryPath);
            NavFiles = _directoryService.Items;
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            ErrorMessage = Strings.Format("LoadWorkspaceError", ex.Message);
        }
    }

    public void Refresh() => _directoryService.LoadDirectory(_directoryService.RootPath);

    [RelayCommand]
    private Task CreateMarkdownNoteAsync() => CreateItemAsync(".md");

    [RelayCommand]
    private Task CreateNoteekNoteAsync() => CreateItemAsync(".noteek");

    [RelayCommand]
    private Task CreateFolderAsync() => CreateItemAsync(null);

    private async Task CreateItemAsync(string? extension)
    {
        try
        {
            var directory = GetTargetDirectory();
            var prompt = extension switch
            {
                ".md" => Strings.NewMarkdownNote,
                ".noteek" => Strings.NewNoteekNote,
                _ => Strings.NewFolder
            };
            var name = await _promptForText(prompt, null);
            if (name is null)
            {
                return;
            }

            if (!IsValidName(name))
            {
                ErrorMessage = Strings.InvalidItemName;
                return;
            }

            if (extension is not null && !name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                name += extension;
            }

            var path = GetChildPath(directory, name);
            EnsureDoesNotExist(path);
            if (extension is null)
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                var content = extension == ".md"
                    ? Strings.NewMarkdownContent
                    : JsonSerializer.Serialize(new InkDocument());
                await File.WriteAllTextAsync(path, content);
            }

            ReloadDirectory();
            ErrorMessage = null;
            if (extension is not null)
            {
                await _openFile(new FileItem(path));
            }
        }
        catch (Exception ex) when (IsFileOperationError(ex))
        {
            ErrorMessage = Strings.Format("CreateItemError", ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySelectedItem))]
    private async Task RenameSelectedAsync()
    {
        if (SelectedItem is not { } item || !_canModifyItem(item))
        {
            return;
        }

        try
        {
            var originalExtension = item.IsDirectory ? string.Empty : Path.GetExtension(item.Name);
            var suggestedName = originalExtension.Length > 0
                ? Path.GetFileNameWithoutExtension(item.Name)
                : item.Name;
            var name = await _promptForText(Strings.Rename, suggestedName);
            if (name is null)
            {
                return;
            }

            if (!IsValidName(name))
            {
                ErrorMessage = Strings.InvalidItemName;
                return;
            }

            if (originalExtension.Length > 0)
            {
                var suppliedExtension = Path.GetExtension(name);
                if (suppliedExtension.Length > 0
                    && !string.Equals(suppliedExtension, originalExtension, StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = Strings.Format("RenameFormatError", originalExtension);
                    return;
                }

                if (suppliedExtension.Length == 0)
                {
                    name += originalExtension;
                }
            }
            else if (!item.IsDirectory && Path.GetExtension(name).Length > 0)
            {
                ErrorMessage = Strings.RenameNoFormatError;
                return;
            }

            var parent = Path.GetDirectoryName(item.FullPath)
                ?? throw new IOException("The selected item has no parent directory.");
            var newPath = GetChildPath(parent, name);
            if (PathsEqual(item.FullPath, newPath))
            {
                return;
            }

            EnsureDoesNotExist(newPath);
            if (item.IsDirectory)
            {
                Directory.Move(item.FullPath, newPath);
            }
            else
            {
                File.Move(item.FullPath, newPath);
            }

            ReloadDirectory();
            ErrorMessage = null;
            ItemRenamed?.Invoke(item.FullPath, newPath);
        }
        catch (Exception ex) when (IsFileOperationError(ex))
        {
            ErrorMessage = Strings.Format("RenameItemError", ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySelectedItem))]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedItem is not { } item || !_canModifyItem(item))
        {
            return;
        }

        try
        {
            if (!await _confirm(Strings.Format("DeleteConfirm", item.Name)))
            {
                return;
            }

            if (item.IsDirectory)
            {
                var isLink = new DirectoryInfo(item.FullPath).Attributes.HasFlag(FileAttributes.ReparsePoint);
                Directory.Delete(item.FullPath, recursive: !isLink);
            }
            else
            {
                File.Delete(item.FullPath);
            }

            ReloadDirectory();
            SelectedItem = null;
            ErrorMessage = null;
            ItemDeleted?.Invoke(item.FullPath);
        }
        catch (Exception ex) when (IsFileOperationError(ex))
        {
            ErrorMessage = Strings.Format("DeleteItemError", ex.Message);
        }
    }

    private string GetTargetDirectory()
    {
        var root = _directoryService.RootPath;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(Strings.NoWorkspace);
        }

        var directory = SelectedItem switch
        {
            { IsDirectory: true } selectedDirectory => selectedDirectory.FullPath,
            { } selectedItem => Path.GetDirectoryName(selectedItem.FullPath)!,
            _ => root
        };
        EnsureWithinWorkspace(directory, allowRoot: true);
        EnsureNoReparsePointAncestor(directory);
        return directory;
    }

    private void EnsureNoReparsePointAncestor(string directory)
    {
        var root = Path.GetFullPath(_directoryService.RootPath);
        var current = Path.GetFullPath(directory);
        while (!PathsEqual(current, root))
        {
            if (new DirectoryInfo(current).Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new IOException(Strings.InvalidItemName);
            }

            current = Path.GetDirectoryName(current)
                ?? throw new IOException(Strings.InvalidItemName);
        }
    }

    private string GetChildPath(string parent, string name)
    {
        var path = Path.GetFullPath(Path.Combine(parent, name));
        EnsureWithinWorkspace(path, allowRoot: false);
        if (!PathsEqual(Path.GetDirectoryName(path) ?? string.Empty, Path.GetFullPath(parent)))
        {
            throw new IOException(Strings.InvalidItemName);
        }

        return path;
    }

    private void EnsureWithinWorkspace(string path, bool allowRoot)
    {
        var root = Path.GetFullPath(_directoryService.RootPath);
        var fullPath = Path.GetFullPath(path);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if ((!allowRoot && string.Equals(fullPath, root, comparison))
            || (!string.Equals(fullPath, root, comparison)
                && !fullPath.StartsWith(
                    root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        + Path.DirectorySeparatorChar,
                    comparison)))
        {
            throw new IOException(Strings.InvalidItemName);
        }
    }

    private static bool IsValidName(string name) =>
        !string.IsNullOrWhiteSpace(name)
        && name is not "." and not ".."
        && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
        && !name.Contains(Path.DirectorySeparatorChar)
        && !name.Contains(Path.AltDirectorySeparatorChar);

    private static void EnsureDoesNotExist(string path)
    {
        if (File.Exists(path) || Directory.Exists(path))
        {
            throw new IOException(Strings.ItemAlreadyExists);
        }
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private void ReloadDirectory()
    {
        var root = _directoryService.RootPath;
        _directoryService.LoadDirectory(root);
        NavFiles = _directoryService.Items;
    }

    private static bool IsFileOperationError(Exception ex) =>
        ex is IOException
            or UnauthorizedAccessException
            or SecurityException
            or ArgumentException
            or NotSupportedException;

    private bool CanModifySelectedItem() =>
        SelectedItem is not null;
}
