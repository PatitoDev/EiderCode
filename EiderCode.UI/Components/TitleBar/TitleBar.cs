using Godot;
using System;

public partial class TitleBar : PanelContainer
{
    public override void _Ready()
    {
        GetNode<Button>("%MinimizeBtn").Pressed += OnMinimizeClicked;
        GetNode<Button>("%MaximizeBtn").Pressed += OnMaximizeeClicked;
        GetNode<Button>("%CloseBtn").Pressed += OnCloseClicked;
    }

    public void OnMinimizeClicked()
    {
      GetWindow().Mode = Window.ModeEnum.Minimized;
    }

    public void OnMaximizeeClicked()
    {
      var window = GetWindow();
      GD.Print(window.Mode);
      if (window.Mode == Window.ModeEnum.Maximized)
      {
        window.Mode = Window.ModeEnum.Windowed;
      }

      if (window.Mode == Window.ModeEnum.Windowed)
      {
        window.Mode = Window.ModeEnum.Maximized;
      }
    }

    public void OnCloseClicked()
    {
      GetTree().Quit();
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);

        if (@event is InputEventMouseButton)
        {
            var mouseEv = (InputEventMouseButton)@event;
            if (mouseEv.ButtonIndex == MouseButton.Left && mouseEv.Pressed)
            {
                GetWindow().StartDrag();
            }
        }
    }
}
