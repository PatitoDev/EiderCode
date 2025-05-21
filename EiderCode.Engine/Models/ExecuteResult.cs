using System.Collections.Generic;
using EiderCode.Engine.Models;

public record ExecuteResult
{
  public EditorPosition? NewCursorPosition { get; init; }
  public ViMode? ChangedMode { get; init; }
  public SubMode? ChangedSubMode { get; init; }
  public Modification? Modification { get; init; }
  // action state after result
  public required ActionState ActionState { get; init; }
}

public record Modification
{
  public required List<string> Lines { get; init; }
  public required EditorPosition StartPosition { get; init; }
}