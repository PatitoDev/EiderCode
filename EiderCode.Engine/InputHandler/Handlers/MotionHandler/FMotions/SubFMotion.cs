using EiderCode.Engine.Models;
using EiderCode.Engine;
using System;

public static class SubFMotion
{
  public static Motion? Handle(InputKey key, EngineState state)
  {
    if (key.Unicode == null) {
      // reset state as invalid path
      return null;
    }

    var currentLine = state.Lines[state.CursorPosition.LineNumber];
    var charToFind = Convert.ToChar(key.Unicode);

    var indexOfChar = currentLine.IndexOf(charToFind, state.CursorPosition.CharNumber);

    return new()
    {
      Start = state.CursorPosition,
      End = state.CursorPosition with { CharNumber = indexOfChar }
    };
  }
}