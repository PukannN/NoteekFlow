using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteekFlow.Models;

namespace NoteekFlow.ViewModels;

public partial class MarkdownViewModel : TextFileViewModel
{
    [ObservableProperty]
    private bool _isPreview;

    public MarkdownViewModel(FileItem fileItem) : base(fileItem) { }

    public bool IsEditing => !IsPreview;

    partial void OnIsPreviewChanged(bool value) => OnPropertyChanged(nameof(IsEditing));

    [RelayCommand]
    private void ShowEdit() => IsPreview = false;

    [RelayCommand]
    private void ShowPreview() => IsPreview = true;
}
