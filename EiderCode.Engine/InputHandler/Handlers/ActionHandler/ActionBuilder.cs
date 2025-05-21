using System;
using System.Collections.Generic;
using EiderCode.Engine.Models;
using EiderCode.Engine;
using System.Linq;

public enum Action {
  Change, // c
  Go,     // g
  Yank,   // y,
  Paste,  // p,
  Replace, // r

  Insert, // i
  Append, // a
}
// x

public record ActionResult
{
  public bool IsReadyToExecute = false;
  public Action? Action = null;
}


public static class ActionBuilder {

  private static IReadOnlyDictionary<long, Action> _actionMap = new Dictionary<long, Action>()
  {
    { (long)Convert.ToInt32('c'), Action.Change },
    { (long)Convert.ToInt32('g'), Action.Go }, // motion not an action
    { (long)Convert.ToInt32('y'), Action.Yank },
    { (long)Convert.ToInt32('p'), Action.Paste },
    { (long)Convert.ToInt32('r'), Action.Replace },

    { (long)Convert.ToInt32('i'), Action.Insert },
    { (long)Convert.ToInt32('a'), Action.Append },
  };

  public static ActionResult GetAction(InputKey key, ActionState state)
  {
    if (
      !key.Unicode.HasValue ||
      !_actionMap.TryGetValue(key.Unicode.Value, out var action)
    ) return new(){
      Action = null,
      IsReadyToExecute = false
    };

    var isReady = false;

    if (
      action == Action.Insert ||
      action == Action.Append
    )
    {
      isReady = true;
      // execute action
    }

    return new(){
      Action = action,
      IsReadyToExecute = isReady
    };
  }


  public static ExecuteResult ExectueAction(
    EngineState engineState,
    ActionState actionState
  ){
    var currentAction = actionState.CurrentAction;
    var cursorPosition = engineState.CursorPosition;
    var lines = engineState.Lines.ToList();
    var motion = actionState.Motion;


    if (currentAction == Action.Change) {
      // handle change - C
      if (motion == null) throw new NotImplementedException();

      // todo - not hardcode this
      var line = lines[motion.End.LineNumber];
      lines[motion.End.LineNumber] = line.Remove(
        motion.Start.CharNumber,
        motion.End.CharNumber - motion.Start.CharNumber
      );

      return new(){
        Modification = new() {
          Lines = lines,
          StartPosition = cursorPosition
        },
        ChangedMode = ViMode.Insert,
        ActionState = new()
      };
    }

    if (currentAction == Action.Insert) {
      return new(){
        ChangedMode = ViMode.Insert,
        ActionState = new()
      };
    }

    if (currentAction == Action.Append) {

      var newCharPosition = Math.Min(
        cursorPosition.CharNumber + 1,
        lines[cursorPosition.LineNumber].Length - 1
      );
      return new(){
        ChangedMode = ViMode.Insert,
        NewCursorPosition = new() {
          CharNumber = newCharPosition,
          LineNumber = cursorPosition.LineNumber
        },
        ActionState = new()
      };
    }

    throw new NotImplementedException();
  }
}
