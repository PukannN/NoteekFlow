using NoteekFlow.Models;
using NoteekFlow.Resources;

namespace NoteekFlow.ViewModels;

public sealed class DefaultWorkspaceViewModel : FileContentViewModel
{
    public string Message => FileItem is null ? Strings.SelectFile : Strings.UnsupportedFile;

    public DefaultWorkspaceViewModel(FileItem? fileItem = null) : base(fileItem) { }
}
