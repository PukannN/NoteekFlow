namespace NoteekFlow.Models;

public sealed class UserPreferences
{
    public string? LastWorkspacePath { get; set; }
    public string Theme { get; set; } = "System";
    public string Language { get; set; } = "en";
    public string AccentColor { get; set; } = "Blue";
    public double ExplorerWidth { get; set; } = 280;
}
