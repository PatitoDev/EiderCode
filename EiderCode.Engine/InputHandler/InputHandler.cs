using EiderCode.Engine;
using EiderCode.Engine.Models;
using Godot;

public static class InputHandler
{
    public static ExecuteResult? Handle(
      InputKey key,
      EngineState state,
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

        switch (state.Mode)
        {
            case ViMode.Normal:
                return NormalModeHandler.Handle(key, state, actionState);

            case ViMode.Insert:
                return InsertModeHandler.Handle(key, state);
        }

      return null;
    }
}