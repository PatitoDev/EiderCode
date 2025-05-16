using System.Collections.Generic;
using EiderCode.Engine.Models;

public record ExecuteResult
{
  public EditorPosition? NewCursorPosition { get; init; }
  public ViMode? ChangedMode  { get; init; }
  public List<string>? Lines { get; init; }
}