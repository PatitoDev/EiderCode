using System.Collections.Generic;
using EiderCode.Engine;
using EiderCode.Engine.Models;


public static class NormalModeHandler
{
    public static ExecuteResult? Handle(
      InputKey key,
      List<string> lines,
      EditorPosition cursorPosition,
      ViMode currentMode,
      ActionState actionState
      )
    {
        var keyChar = key.ToString()[0];
        // motions are always either first or last and always execute
        var motion = MotionBuilder.HandleMotion(
          key,
          lines,
          cursorPosition
        );

        // handle stack
        if (motion != null)
        {
            return OnMotion(
              motion,
              key,
              lines,
              cursorPosition,
              currentMode,
              actionState
            );
        }

        // actions can repeat or be first
        var action = ActionBuilder.GetAction(key, actionState);
        if (action.Action != null)
        {
            var stateWithAction = actionState with
            {
                CurrentAction = action.Action
            };

            // add action to stack or execute it if ready
            if (action.IsReadyToExecute)
            {
                // ready to execute so do action
                var result = ActionBuilder.ExectueAction(
                  cursorPosition,
                  stateWithAction,
                  currentMode,
                  lines
                );
                return result;
            }
            return new()
            {
                ActionState = stateWithAction,
            };
        }

        // no motion or action so reset state
        return new()
        {
            ActionState = new()
        };
    }

    private static ExecuteResult? OnMotion(
      Motion motion,
      InputKey key,
      List<string> lines,
      EditorPosition cursorPosition,
      ViMode currentMode,
      ActionState state
    )
    {
        if (state.CurrentAction == null)
        {
            // if we don't have a action then we just move around
            return new()
            {
                NewCursorPosition = motion.End,
                ActionState = new() // reset state as we completed the action
            };
        }

        // if we have an current action then the motion completes the execution
        var actionStateWithMotion = state with
        {
            Motion = motion
        };

        return ActionBuilder.ExectueAction(
          cursorPosition,
          actionStateWithMotion,
          currentMode,
          lines
        );
    }
}