using Godot;
using System;

namespace EiderCode.UI;

public enum CursorType {
  Block,
  Line
}

public partial class Cursor : Panel
{
  public Vector2 BlockSize;

  public override void _Ready()
  {
    Size = BlockSize;
  }

  private CursorType _cursorType;

  public void MoveTo(Vector2 position)
  {
    var t = GetTree().CreateTween();
    t.SetTrans(Tween.TransitionType.Spring);
    t.SetEase(Tween.EaseType.InOut);
    t.TweenProperty(this, "global_position", position, 0.05);
  }

  public void SetCursorType(CursorType cursorType)
  {
    var t = GetTree().CreateTween();
    t.SetTrans(Tween.TransitionType.Spring);
    t.SetEase(Tween.EaseType.InOut);
    _cursorType = cursorType;
    if (cursorType == CursorType.Line) {
      t.TweenProperty(this, "size:x", 3, 0.10);
      t.TweenProperty(this, "size:y", BlockSize.Y, 0.10);
    } else {
      t.TweenProperty(this, "size:x", BlockSize.X, 0.10);
      t.TweenProperty(this, "size:y", BlockSize.Y, 0.10);
    }
  }
}
