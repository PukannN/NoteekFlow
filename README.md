# NoteekFlow

NoteekFlow is a cross-platform note-taking app built with Avalonia and .NET 10. Its
workspace UI is inspired by desktop editors: a top navigation bar, a resizable
nested file explorer, and a tabbed editor that fills the remaining space.

## Current features

- Open a workspace folder and browse nested folders.
- Create Markdown (`.md`) and Noteek (`.noteek`) notes.
- Open, edit, and save Markdown, Noteek, and text files.
- Switch Markdown notes between editing and rendered preview.
- Draw pressure-sensitive ink in Noteek files with a stylus, pen, or mouse; switch between drawing and non-editing preview.
- Preview common image files and navigate rendered PDF pages.
- Close editor tabs and switch between open files.

Noteek documents are JSON with a `version` and an `elements` array. Ink strokes
store ordered points with canvas coordinates and pressure.

UI messages are stored in `.resx` resources under `Resources/`; `Strings.cs.resx`
provides the Czech translation, and additional cultures can be added with the
same resource keys.

The Settings menu offers system, dark, and light themes. NoteekFlow stores
preferences in the user's local application data folder, including the last
successfully opened workspace, which is restored on the next launch.

## Project structure

- `Models/` contains the `FileSystemItem` hierarchy (`DirectoryItem` and `FileItem`), ink data, and directory loading.
- `ViewModels/` contains the workspace, navigation, explorer, and editor state.
- `Views/` contains the application shell and format-specific editor views.
- `Controls/` contains reusable tab, thumbnail, and stylus-ink controls.
