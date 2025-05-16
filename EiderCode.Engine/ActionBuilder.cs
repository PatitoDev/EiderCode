using System;
using System.Collections.Generic;
using EiderCode.Engine.Models;
using Godot;

public enum Action {
  Change, // c
  Go,     // g
  Yank,   // y,
  Paste,  // p,
  Replace, // r

  Insert, // i
  Append, // a
}

public record ActionResult
{
  public bool IsReadyToExecute = false;
  public Action? Action = null;
}


public static class ActionBuilder {

  private static IReadOnlyDictionary<long, Action> _actionMap = new Dictionary<long, Action>()
  {
    { (long)Convert.ToInt32('c'), Action.Change },
    { (long)Convert.ToInt32('g'), Action.Go },
    { (long)Convert.ToInt32('y'), Action.Yank },
    { (long)Convert.ToInt32('p'), Action.Paste },
    { (long)Convert.ToInt32('r'), Action.Replace },

    { (long)Convert.ToInt32('i'), Action.Insert },
    { (long)Convert.ToInt32('a'), Action.Append },
  };

  public static ActionResult GetAction(InputKey key, ViStack stack)
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
    EditorPosition cursorPosition,
    ViStack stack,
    ViMode mode,
    List<string> lines
  ){

    if (stack.CurrentAction == Action.Change) {
      if (stack.Motion == null) throw new Exception("invalid path");

      // todo - not hardcode this
      var line = lines[stack.Motion.End.LineNumber];
      lines[stack.Motion.End.LineNumber] = line.Remove(
        stack.Motion.Start.CharNumber,
        stack.Motion.End.CharNumber - stack.Motion.Start.CharNumber
      );

      return new(){
        Lines = lines,
        ChangedMode = ViMode.Insert,
      };
    }

    if (stack.CurrentAction == Action.Insert) {
      return new(){ ChangedMode = ViMode.Insert };
    }

    if (stack.CurrentAction == Action.Append) {

      var newCharPosition = Math.Min(
        cursorPosition.CharNumber + 1,
        lines[cursorPosition.LineNumber].Length - 1
      );
      return new(){
        ChangedMode = ViMode.Insert,
        NewCursorPosition = new() {
          CharNumber = newCharPosition,
          LineNumber = cursorPosition.LineNumber
        }
      };
    }

    throw new NotImplementedException();
  }
}
