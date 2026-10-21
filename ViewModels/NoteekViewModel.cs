using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using System.Linq;
using System.IO;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteekFlow.Models;
using NoteekFlow.Resources;

namespace NoteekFlow.ViewModels;

public partial class NoteekViewModel : ZoomableFileContentViewModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    private const string LegacyEmptyDocument = "{\"version\",\"elements\":[]}";
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private ObservableCollection<InkStroke>? _observedStrokes;
    private bool _isLoading;
    private int _changeVersion;
    private int _autoSaveWorker;

    [ObservableProperty]
    private ObservableCollection<InkStroke> _strokes = new();

    [ObservableProperty]
    private bool _isPreview;

    [ObservableProperty]
    private string _currentTool = "Pen";

    [ObservableProperty]
    private string _strokeColor = "#20242B";

    [ObservableProperty]
    private string _backgroundColor = "#FBFAF7";

    [ObservableProperty]
    private bool _showGrid;

    [ObservableProperty]
    private double _strokeWidth = 3;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsEditing => !IsPreview;

    public NoteekViewModel(FileItem fileItem) : base(fileItem) { }

    partial void OnIsPreviewChanged(bool value) => OnPropertyChanged(nameof(IsEditing));
    partial void OnBackgroundColorChanged(string value) => MarkChanged();
    partial void OnShowGridChanged(bool value) => MarkChanged();
    partial void OnStrokeWidthChanged(double value) => MarkChanged();

    partial void OnStrokesChanged(ObservableCollection<InkStroke> value)
    {
        if (_observedStrokes is not null)
        {
            _observedStrokes.CollectionChanged -= OnElementsChanged;
            foreach (var element in _observedStrokes)
            {
                element.Points.CollectionChanged -= OnElementPointsChanged;
            }
        }

        _observedStrokes = value;
        value.CollectionChanged += OnElementsChanged;
        foreach (var element in value)
        {
            ObserveElement(element);
        }

        IsDirty = false;
    }

    public async Task LoadAsync()
    {
        if (FileItem is null)
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(FileItem.FullPath);
            _isLoading = true;
            if (string.Concat(json.Where(character => !char.IsWhiteSpace(character))) == LegacyEmptyDocument)
            {
                Strokes = new ObservableCollection<InkStroke>();
                _isLoading = false;
                IsDirty = true;
                ErrorMessage = Strings.LegacyNoteRecovered;
                return;
            }

            var document = JsonSerializer.Deserialize<InkDocument>(json, JsonOptions);
            document ??= new InkDocument();
            Strokes = document.Elements ?? new ObservableCollection<InkStroke>();
            BackgroundColor = document.BackgroundColor;
            ShowGrid = document.ShowGrid;
            StrokeWidth = Math.Clamp(document.StrokeWidth, 1, 30);
            _isLoading = false;
            IsDirty = false;
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or JsonException)
        {
            _isLoading = false;
            ErrorMessage = Strings.Format("ReadFileError", ex.Message);
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (FileItem is null)
        {
            return;
        }

        var lockAcquired = false;
        try
        {
            await _saveLock.WaitAsync();
            lockAcquired = true;
            var savedVersion = _changeVersion;
            var document = new InkDocument
            {
                Elements = Strokes,
                BackgroundColor = BackgroundColor,
                ShowGrid = ShowGrid,
                StrokeWidth = StrokeWidth
            };
            var json = JsonSerializer.Serialize(document, JsonOptions);
            var filePath = Path.GetFullPath(FileItem.FullPath);
            var temporaryPath = filePath + ".tmp";
            var bytes = Encoding.UTF8.GetBytes(json);
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, filePath, overwrite: true);
            IsDirty = _changeVersion != savedVersion;
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            ErrorMessage = Strings.Format("SaveFileError", ex.Message);
        }
        finally
        {
            if (lockAcquired)
            {
                _saveLock.Release();
            }
        }
    }

    [RelayCommand]
    private void ShowEdit() => IsPreview = false;

    [RelayCommand]
    private void ShowPreview() => IsPreview = true;

    [RelayCommand]
    private void SetTool(string tool) => CurrentTool = tool;

    [RelayCommand]
    private void SetStrokeColor(string color) => StrokeColor = color;

    [RelayCommand]
    private void SetBackgroundColor(string color) => BackgroundColor = color;

    [RelayCommand]
    private void IncreaseStrokeWidth() => StrokeWidth = Math.Min(30, StrokeWidth + 1);

    [RelayCommand]
    private void DecreaseStrokeWidth() => StrokeWidth = Math.Max(1, StrokeWidth - 1);

    [RelayCommand]
    private void ClearCanvas()
    {
        if (Strokes.Count > 0)
        {
            Strokes.Clear();
            IsDirty = true;
        }
    }

    public void AddText(Point position, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var element = new InkStroke
        {
            Kind = "Text",
            Color = StrokeColor,
            Text = text,
            Width = 20
        };
        element.Points.Add(new InkPoint(position.X, position.Y));
        Strokes.Add(element);
    }

    private void OnElementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (InkStroke element in e.OldItems)
            {
                element.Points.CollectionChanged -= OnElementPointsChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (InkStroke element in e.NewItems)
            {
                ObserveElement(element);
            }
        }

        MarkChanged();
    }

    private void ObserveElement(InkStroke element) =>
        element.Points.CollectionChanged += OnElementPointsChanged;

    private void OnElementPointsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        MarkChanged();

    private void MarkChanged()
    {
        if (_isLoading)
        {
            return;
        }

        _changeVersion++;
        IsDirty = true;
        StartAutoSaveWorker();
    }

    private void StartAutoSaveWorker()
    {
        if (Interlocked.CompareExchange(ref _autoSaveWorker, 1, 0) == 0)
        {
            _ = SaveAfterEditAsync();
        }
    }

    private async Task SaveAfterEditAsync()
    {
        var attemptedVersion = _changeVersion;
        try
        {
            while (true)
            {
                await Task.Delay(500);
                var currentVersion = Volatile.Read(ref _changeVersion);
                if (currentVersion != attemptedVersion)
                {
                    attemptedVersion = currentVersion;
                    continue;
                }

                await SaveAsync();
                if (currentVersion == Volatile.Read(ref _changeVersion))
                {
                    return;
                }

                attemptedVersion = Volatile.Read(ref _changeVersion);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _autoSaveWorker, 0);
            if (Volatile.Read(ref _changeVersion) != attemptedVersion)
            {
                StartAutoSaveWorker();
            }
        }
    }
}
