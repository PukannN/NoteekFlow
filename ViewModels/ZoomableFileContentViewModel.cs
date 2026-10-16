using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteekFlow.Models;

namespace NoteekFlow.ViewModels;

public abstract partial class ZoomableFileContentViewModel : FileContentViewModel
{
    private const double MinimumZoom = 0.25;
    private const double MaximumZoom = 4;

    [ObservableProperty]
    private double _zoom = 1;

    protected ZoomableFileContentViewModel(FileItem fileItem) : base(fileItem) { }

    public string ZoomLabel => $"{Zoom:P0}";

    partial void OnZoomChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomLabel));
        OnZoomLevelChanged();
    }

    protected virtual void OnZoomLevelChanged() { }

    [RelayCommand]
    private void ZoomIn() => Zoom = Math.Min(MaximumZoom, Math.Round(Zoom + 0.1, 2));

    [RelayCommand]
    private void ZoomOut() => Zoom = Math.Max(MinimumZoom, Math.Round(Zoom - 0.1, 2));

    [RelayCommand]
    private void ResetZoom() => Zoom = 1;
}
