using System.Collections.Generic;


public record IconDefinition {
  public required string IconPath { get; set; }
}

public record IconTheme {
  public required string File { get; set; }
  public required string Folder { get; set; }
  public required string FolderExpanded { get; set; }
  public required string RootFolder { get; set; }
  public required string RootFolderExpanded { get; set; }

  public required Dictionary<string, string> LanguageIds { get; set; }
  public required Dictionary<string, string> FileExtensions { get; set; }
  public required Dictionary<string, string> FileNames { get; set; }
  public required Dictionary<string, string> FolderNames { get; set; }
  public required Dictionary<string, string> FolderNamesExpanded { get; set; }
  public required Dictionary<string, IconDefinition> IconDefinitions { get; set; }
}
