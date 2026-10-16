using System;
using System.IO;
using System.Security;
using System.Text.Json;

namespace NoteekFlow.Models;

public sealed class UserPreferencesStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;

    public UserPreferencesStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NoteekFlow",
            "settings.json");
    }

    public UserPreferences Load()
    {
        if (!File.Exists(_filePath))
        {
            return new UserPreferences();
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
    }

    public void Save(UserPreferences preferences)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new IOException("The preferences file path does not have a parent directory.");
        }

        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(preferences, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public static bool IsRecoverableLoadError(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or SecurityException or JsonException;
}
