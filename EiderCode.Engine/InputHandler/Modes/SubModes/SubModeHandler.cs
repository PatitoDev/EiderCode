using Godot;
using EiderCode.Engine;
using EiderCode.Engine.Models;

/*
Sub modes
  g - go
  f - find
*/


public static class SubModeHandler
{
  public static ExecuteResult? Handle(InputKey key, EngineState engineState, ActionState actionState)
  {
    // if we have an action we can use inner and around and surround sub modes
     if (actionState.CurrentAction != null) {
      switch (key) {
        case { KeyCode: Key.I, IsShiftPressed: false, IsAltPressed: false, IsControlPressed: false }:
          return new () {
            ActionState = actionState,
            ChangedSubMode = SubMode.TextObjectInside
        };
        case { KeyCode: Key.A, IsShiftPressed: false, IsAltPressed: false, IsControlPressed: false }:
          return new () {
            ActionState = actionState,
            ChangedSubMode = SubMode.TextObjectAround
        };
        case { KeyCode: Key.S, IsShiftPressed: false, IsAltPressed: false, IsControlPressed: false }:
          return new () {
            ActionState = actionState,
            ChangedSubMode = SubMode.TextObjectSurround
        };
      }
    }

    switch (key)
    {
      case { KeyCode: Key.G, IsShiftPressed: false, IsAltPressed: false, IsControlPressed: false }:
        return new () {
          ActionState = actionState,
          ChangedSubMode = SubMode.Go
        };
      case { KeyCode: Key.F, IsShiftPressed: false, IsAltPressed: false, IsControlPressed: false }:
        return new () {
          ActionState = actionState,
          ChangedSubMode = SubMode.FindFordward
      };
      case { KeyCode: Key.F, IsShiftPressed: true, IsAltPressed: false, IsControlPressed: false }:
        return new () {
          ActionState = actionState,
          ChangedSubMode = SubMode.FindBackwards
      };
      case { KeyCode: Key.R, IsShiftPressed: false, IsAltPressed: false, IsControlPressed: false }:
        return new () {
          ActionState = actionState,
          ChangedSubMode = SubMode.ReplaceChar
      };

      default:
        return null;
    }

  }
}