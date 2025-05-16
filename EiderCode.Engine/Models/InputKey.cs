using Godot;

namespace EiderCode.Engine.Models;

public record InputKey {
  public Key KeyCode { get; init; }
  public bool IsShiftPressed { get; init; }
  public bool IsControlPressed { get; init; }
  public bool IsAltPressed { get; init; }
  public long? Unicode  { get; init; }

  public InputKey(){}

  public InputKey(
    Key keyCode,
    bool isShiftPressed,
    bool isControlPressed,
    bool isAltPressed,
    long? unicode
  )
  {
    KeyCode = keyCode;
    IsShiftPressed = isShiftPressed;
    IsControlPressed = isControlPressed;
    IsAltPressed = isAltPressed;
    Unicode = unicode;
  }
}
