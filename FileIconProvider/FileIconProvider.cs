using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;


public record FileIcon
{
    public required CompressedTexture2D Texture { init; get; }
}

public class FileIconProvider
{
    private IReadOnlyList<string> _iconThemes = new List<string>() {
      "frappe",
      "latte",
      "macchiato",
      "mocha"
    };

    private IconTheme _iconTheme;
    private Dictionary<string, FileIcon> _iconTextureMap;

    public FileIconProvider()
    {
        _loadIconTheme(3);
    }

    private void _loadIconTheme(int themeId)
    {
        var themeJson = GD.Load<Json>($"res://Icons/catppuccin/{_iconThemes[themeId]}/theme.json");
        var themeString = themeJson.Data.As<string>();

        _iconTheme = JsonSerializer.Deserialize<IconTheme>(themeString, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        })!;

        _iconTextureMap = _iconTheme.IconDefinitions
          .ToDictionary(
          item => item.Key,
          item =>
          {
              var icon = GD.Load<CompressedTexture2D>(
                "res://" + Path.Combine($"/Icons/catppuccin/{_iconThemes[themeId]}", item.Value.IconPath)
              );

              return new FileIcon() { Texture = icon };
          }
          );
    }

    public FileIcon GetFolderIcon(string fileName, bool isOpen, bool isRoot)
    {
        if (isRoot)
        {
            return isOpen ?
              _iconTextureMap[_iconTheme.RootFolderExpanded] :
              _iconTextureMap[_iconTheme.RootFolder];
        }

        var iconKey = isOpen ? _iconTheme.FolderExpanded : _iconTheme.Folder;

        if (isOpen)
        {
            if (_iconTheme.FolderNamesExpanded.TryGetValue(fileName, out var folderNameOpenExpanded))
            {
                iconKey = folderNameOpenExpanded;
            }
        }
        else
        {
            if (_iconTheme.FolderNames.TryGetValue(fileName, out var folderNameOpen))
            {
                iconKey = folderNameOpen;
            }
        }

        return _iconTextureMap[iconKey];
    }

    public FileIcon GetFileIcon(string fileName)
    {
        if (_iconTheme.FileNames.TryGetValue(fileName, out var fileNameMatch))
        {
            return _iconTextureMap[fileNameMatch];
        }

        var matchedExtension = _iconTheme.FileExtensions
          .Where(ext => fileName.EndsWith($".{ext.Key}"))
          .OrderBy(ext => ext.Key.Length)
          .Select(ext => ext.Value)
          .FirstOrDefault();

        if (
          matchedExtension != null &&
          _iconTextureMap.TryGetValue(matchedExtension, out var fileTypeFromExtension))
        {
            return fileTypeFromExtension;
        }

        return _iconTextureMap[_iconTheme.File];
    }
}