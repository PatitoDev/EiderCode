using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;


public record FileIcon
{
    public required CompressedTexture2D Texture { init; get; }
}

public enum IconThemeKey {
    Frappe,
    Latte,
    Macchiato,
    Mocha
}



public class FileIconProvider
{
    private IReadOnlyDictionary<IconThemeKey, string> _iconThemeKeyMap = new Dictionary<IconThemeKey, string>(){
        { IconThemeKey.Frappe, "frappe" },
        { IconThemeKey.Latte, "latte" },
        { IconThemeKey.Macchiato, "macchiato" },
        { IconThemeKey.Mocha, "mocha" },
    };

    public bool HasLoaded { get; private set; } = false;
    private IconTheme? _iconTheme;
    private Dictionary<string, FileIcon>? _iconTextureMap;

    public FileIconProvider()
    {
    }

    public void LoadIconTheme(IconThemeKey iconThemeKey)
    {
        HasLoaded = false;
        var themeJson = GD.Load<Json>($"res://Icons/catppuccin/{_iconThemeKeyMap[iconThemeKey]}/theme.json");
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
                "res://" + Path.Combine($"/Icons/catppuccin/{_iconThemeKeyMap[iconThemeKey]}", item.Value.IconPath)
              );

              return new FileIcon() { Texture = icon };
          }
        );
        HasLoaded = true;
    }

    public FileIcon GetFolderIcon(string fileName, bool isOpen, bool isRoot)
    {
        if (_iconTheme == null || _iconTextureMap == null)
            throw new Exception("Icons not loaded");

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

        if (_iconTheme == null || _iconTextureMap == null)
            throw new Exception("Icons not loaded");

        if (_iconTheme.FileNames.TryGetValue(fileName, out var fileNameMatch))
        {
            return _iconTextureMap[fileNameMatch];
        }

        var extension = fileName.GetExtension();
        if (_iconTheme.FileExtensions.TryGetValue(extension, out var matchedExtension)){
            if (_iconTextureMap.TryGetValue(matchedExtension, out var fileTypeFromExtension))
            {
                return fileTypeFromExtension;
            }
        }

        return _iconTextureMap[_iconTheme.File];
    }
}