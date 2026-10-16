using System.Globalization;
using System.Resources;

namespace NoteekFlow.Resources;

public static class Strings
{
    private static readonly ResourceManager ResourceManager =
        new("NoteekFlow.Resources.Strings", typeof(Strings).Assembly);

    public static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture)
        ?? throw new MissingManifestResourceException($"Missing localization resource '{name}'.");

    public static string Format(string name, params object[] values) =>
        string.Format(CultureInfo.CurrentUICulture, Get(name), values);

    public static string WelcomeTitle => Get(nameof(WelcomeTitle));
    public static string SelectFile => Get(nameof(SelectFile));
    public static string UnsupportedFile => Get(nameof(UnsupportedFile));
    public static string Explorer => Get(nameof(Explorer));
    public static string Edit => Get(nameof(Edit));
    public static string Preview => Get(nameof(Preview));
    public static string OpenWorkspace => Get(nameof(OpenWorkspace));
    public static string CreateNote => Get(nameof(CreateNote));
    public static string CreateNoteTooltip => Get(nameof(CreateNoteTooltip));
    public static string ToggleExplorer => Get(nameof(ToggleExplorer));
    public static string Save => Get(nameof(Save));
    public static string CloseTab => Get(nameof(CloseTab));
    public static string PdfPreview => Get(nameof(PdfPreview));
    public static string NoWorkspace => Get(nameof(NoWorkspace));
    public static string Ready => Get(nameof(Ready));
    public static string PreviousPage => Get(nameof(PreviousPage));
    public static string NextPage => Get(nameof(NextPage));
    public static string ClearCanvas => Get(nameof(ClearCanvas));
    public static string CanvasHint => Get(nameof(CanvasHint));
    public static string FolderAccessUnavailable => Get(nameof(FolderAccessUnavailable));
    public static string LocalWorkspaceRequired => Get(nameof(LocalWorkspaceRequired));
    public static string FileAccessUnavailable => Get(nameof(FileAccessUnavailable));
    public static string SaveBeforeClose => Get(nameof(SaveBeforeClose));
    public static string SaveFailed => Get(nameof(SaveFailed));
    public static string Saved => Get(nameof(Saved));
    public static string ImageLoadError => Get(nameof(ImageLoadError));
    public static string NewMarkdownContent => Get(nameof(NewMarkdownContent));
    public static string CannotSaveActiveFile => Get(nameof(CannotSaveActiveFile));
    public static string PdfPlatformUnsupported => Get(nameof(PdfPlatformUnsupported));
    public static string OpenFileError => Get(nameof(OpenFileError));
    public static string LegacyNoteRecovered => Get(nameof(LegacyNoteRecovered));
    public static string Settings => Get(nameof(Settings));
    public static string ThemeSystem => Get(nameof(ThemeSystem));
    public static string ThemeDark => Get(nameof(ThemeDark));
    public static string ThemeLight => Get(nameof(ThemeLight));
    public static string ExplorerContextHelp => Get(nameof(ExplorerContextHelp));
    public static string ResizeExplorer => Get(nameof(ResizeExplorer));
    public static string SelectTab => Get(nameof(SelectTab));
    public static string NewMarkdownNote => Get(nameof(NewMarkdownNote));
    public static string NewNoteekNote => Get(nameof(NewNoteekNote));
    public static string NewFolder => Get(nameof(NewFolder));
    public static string Rename => Get(nameof(Rename));
    public static string Delete => Get(nameof(Delete));
    public static string EnterName => Get(nameof(EnterName));
    public static string InvalidItemName => Get(nameof(InvalidItemName));
    public static string ItemAlreadyExists => Get(nameof(ItemAlreadyExists));
    public static string CannotDeleteDirtyFile => Get(nameof(CannotDeleteDirtyFile));
    public static string CreateMarkdownTooltip => Get(nameof(CreateMarkdownTooltip));
    public static string CreateNoteekTooltip => Get(nameof(CreateNoteekTooltip));
    public static string CreateFolderTooltip => Get(nameof(CreateFolderTooltip));
    public static string RenameTooltip => Get(nameof(RenameTooltip));
    public static string DeleteTooltip => Get(nameof(DeleteTooltip));
    public static string Cancel => Get(nameof(Cancel));
    public static string Confirm => Get(nameof(Confirm));
    public static string SaveTooltip => Get(nameof(SaveTooltip));
    public static string MarkdownEditTooltip => Get(nameof(MarkdownEditTooltip));
    public static string MarkdownPreviewTooltip => Get(nameof(MarkdownPreviewTooltip));
    public static string CanvasEditTooltip => Get(nameof(CanvasEditTooltip));
    public static string CanvasPreviewTooltip => Get(nameof(CanvasPreviewTooltip));
    public static string OpenWorkspaceTooltip => Get(nameof(OpenWorkspaceTooltip));
    public static string WorkspacePickerTitle => Get(nameof(WorkspacePickerTitle));
    public static string NoteFileType => Get(nameof(NoteFileType));
    public static string CreateNoteDialogTitle => Get(nameof(CreateNoteDialogTitle));
    public static string NewNoteFileName => Get(nameof(NewNoteFileName));
    public static string RenameFormatError => Get(nameof(RenameFormatError));
    public static string RenameNoFormatError => Get(nameof(RenameNoFormatError));
    public static string ThemeSettings => Get(nameof(ThemeSettings));
    public static string PenTool => Get(nameof(PenTool));
    public static string RectangleTool => Get(nameof(RectangleTool));
    public static string EllipseTool => Get(nameof(EllipseTool));
    public static string LineTool => Get(nameof(LineTool));
    public static string TextTool => Get(nameof(TextTool));
    public static string EraserTool => Get(nameof(EraserTool));
    public static string EnterCanvasText => Get(nameof(EnterCanvasText));
    public static string AccentColor => Get(nameof(AccentColor));
    public static string AccentBlue => Get(nameof(AccentBlue));
    public static string AccentGreen => Get(nameof(AccentGreen));
    public static string AccentPurple => Get(nameof(AccentPurple));
    public static string ExplorerWidth => Get(nameof(ExplorerWidth));
    public static string ExplorerCompact => Get(nameof(ExplorerCompact));
    public static string ExplorerStandard => Get(nameof(ExplorerStandard));
    public static string ExplorerWide => Get(nameof(ExplorerWide));
    public static string InkDark => Get(nameof(InkDark));
    public static string InkRed => Get(nameof(InkRed));
    public static string InkBlue => Get(nameof(InkBlue));
    public static string InkGreen => Get(nameof(InkGreen));
    public static string Language => Get(nameof(Language));
    public static string LanguageEnglish => Get(nameof(LanguageEnglish));
    public static string LanguageCzech => Get(nameof(LanguageCzech));
    public static string Grid => Get(nameof(Grid));
    public static string StrokeWidth => Get(nameof(StrokeWidth));
    public static string BackgroundLight => Get(nameof(BackgroundLight));
    public static string BackgroundWhite => Get(nameof(BackgroundWhite));
    public static string BackgroundDark => Get(nameof(BackgroundDark));
    public static string ZoomIn => Get(nameof(ZoomIn));
    public static string ZoomOut => Get(nameof(ZoomOut));
    public static string ResetZoom => Get(nameof(ResetZoom));
    public static string ConfirmCanvasClear => Get(nameof(ConfirmCanvasClear));
}
