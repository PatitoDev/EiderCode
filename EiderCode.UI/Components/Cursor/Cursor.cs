using Godot;
using System;

namespace EiderCode.UI;

public enum CursorType {
  Block,
  Line
}

public partial class Cursor : Panel
{
  private CursorType _cursorType;

  public void MoveTo(Vector2 position)
  {
    GlobalPosition = position;
  }

  public void SetBlockSize(Vector2 size)
  {
    Size = size;
  }

  public void SetCursorType(CursorType cursorType)
  {
    _cursorType = cursorType;
  }
}
