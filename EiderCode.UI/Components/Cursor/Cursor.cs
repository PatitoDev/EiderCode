using Godot;

namespace EiderCode.UI;

public enum CursorType {
  Block,
  Line
}

public partial class Cursor : Panel
{
  public Vector2 BlockSize;
  private Tween? _sizeTween = null;
  private Tween? _moveTween = null;

  public override void _Ready()
  {
    Size = BlockSize;
  }

  private CursorType _cursorType;

  public void MoveTo(Vector2 position)
  {
    _moveTween?.Kill();

    _moveTween = GetTree().CreateTween();
    _moveTween.SetTrans(Tween.TransitionType.Spring);
    _moveTween.SetEase(Tween.EaseType.InOut);
    _moveTween.TweenProperty(
      this,
      "position",
      position - new Vector2(0, Size.Y),
      0.05
    );
  }

  public void SetCursorType(CursorType cursorType)
  {
    _sizeTween?.Kill();
    _cursorType = cursorType;

    _sizeTween = GetTree().CreateTween();
    _sizeTween.SetTrans(Tween.TransitionType.Spring);
    _sizeTween.SetEase(Tween.EaseType.InOut);
    if (cursorType == CursorType.Line) {
      _sizeTween.TweenProperty(this, "size:x", 3, 0.10);
      _sizeTween.TweenProperty(this, "size:y", BlockSize.Y, 0.10);
    } else {
      _sizeTween.TweenProperty(this, "size:x", BlockSize.X, 0.10);
      _sizeTween.TweenProperty(this, "size:y", BlockSize.Y, 0.10);
    }
  }

  public void UpdateCursorSizeAndBounds(Vector2 newPosition, Vector2 size)
  {
    MoveTo(newPosition + new Vector2(0, 5));
    BlockSize = size;
    SetCursorType(_cursorType); // force re-render
  }
}
