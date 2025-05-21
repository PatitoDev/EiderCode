using EiderCode.Engine;
using EiderCode.Engine.Models;


public static class NormalModeHandler
{
    public static ExecuteResult? Handle(
      InputKey key,
      EngineState engineState,
      ActionState actionState
      )
    {
        var keyChar = key.ToString()[0];

        if (engineState.SubMode == null) {
            var subModeResult = SubModeHandler.Handle(key, engineState, actionState);
            if (subModeResult != null)
            {
                return subModeResult;
            }
        }

        // motions are always either first or last and always execute
        var motion = MotionBuilder.HandleMotion(key, engineState);

        // handle stack
        if (motion != null)
        {
            return OnMotion(motion, engineState, actionState);
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
                var result = ActionBuilder.ExectueAction(engineState, stateWithAction);
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
      EngineState engineState,
      ActionState actionState
    )
    {
        if (actionState.CurrentAction == null)
        {
            // if we don't have a action then we just move around
            return new()
            {
                NewCursorPosition = motion.End,
                ChangedSubMode = null, // reset sub mode
                ActionState = new() // reset state as we completed the action
            };
        }

        // if we have an current action then the motion completes the execution
        var actionStateWithMotion = actionState with
        {
            Motion = motion,
        };

        return ActionBuilder.ExectueAction(engineState, actionStateWithMotion);
    }
}