using System.Collections.Generic;
using EiderCode.Engine.Models;
using Godot;

public static class InputHandler
{
    public static ExecuteResult? Handle(
      InputKey key,
      ViMode currentMode,
      List<string> lines,
      EditorPosition cursorPosition,
      ActionState actionState
    )
    {
        /*
        GD.Print("Code: ", key.KeyCode);
        GD.Print("Unicode: ", key.Unicode);
        GD.Print("IsShifted: ", key.IsShiftPressed);
        GD.Print("IsControlPressed: ", key.IsControlPressed);
        */

        if (key.KeyCode == Key.Escape)
        {
            return new()
            {
                ChangedMode = ViMode.Normal,
                ActionState = new() // reset state
            };
        }

        switch (currentMode)
        {
            case ViMode.Normal:
                return NormalModeHandler
                  .Handle(key, lines, cursorPosition, currentMode, actionState);

            case ViMode.Insert:
                return InsertModeHandler
                  .Handle(key, lines, cursorPosition);
        }

      return null;
    }
}