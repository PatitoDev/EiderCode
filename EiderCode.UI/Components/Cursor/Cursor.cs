using Godot;
using System;

namespace EiderCode.UI;

public enum CursorType {
  Block,
  Line
}

public partial class Cursor : Panel
{
  private Label? label;

  public override void _Ready()
  {
    label = GetNode<Label>("%Label");
    var bounds = label.GetCharacterBounds(0);
    Size = new Vector2(20, 35);
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
      var bounds = label!.GetCharacterBounds(0);
      label!.Visible = false;
      t.TweenProperty(this, "size:x", 3, 0.10);
      t.TweenProperty(this, "size:y", bounds.Size.Y, 0.10);
    } else {
      label!.Visible = true;
      var bounds = label.GetCharacterBounds(0);
      t.TweenProperty(this, "size:x", bounds.Size.X, 0.10);
      t.TweenProperty(this, "size:y", bounds.Size.Y, 0.10);
    }
  }

  public void SetChar(char character)
  {
    if (label == null) return;
    label.Text = character.ToString();
    UpdateBounds();
  }


  private void UpdateBounds()
  {
    if (label == null) return;
    var bounds = label.GetCharacterBounds(0);
    if (_cursorType == CursorType.Block){
      Size = bounds.Size;
    } else {
      Size = new Vector2(3, Size.Y);
    }
  }
}
