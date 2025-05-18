using Godot;
using System;

public partial class TitleBar : PanelContainer
{
  private bool _isPressed = false;
  private Vector2I _pressedOffset = Vector2I.Zero;

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);
        if (@event is InputEventMouseButton) {
          var mouseEv = (InputEventMouseButton) @event;
          if (mouseEv.ButtonIndex == MouseButton.Left)
          {
            _isPressed = mouseEv.IsPressed();
            _pressedOffset = (Vector2I)mouseEv.GlobalPosition;
          }
        }

        if (
          _isPressed &&
          @event is InputEventMouseMotion
        ) {
          var mouseEv = (InputEventMouseMotion) @event;
          var mousePos = DisplayServer.MouseGetPosition();
          GetWindow().Position = mousePos - _pressedOffset;
          //DisplayServer.WindowSetPosition(mousePos - _pressedOffset);
        }
    }
}
