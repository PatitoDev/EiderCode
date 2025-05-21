using System;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionL : IMotion
{
    public static Motion? Handle(InputKey key, EngineState state)
    {
        var currentLineLength = state.Lines[state.CursorPosition.LineNumber].Length;

        return new Motion()
        {
            Start = new()
            {
                CharNumber = state.CursorPosition.CharNumber,
                LineNumber = state.CursorPosition.LineNumber,
            },
            End = new()
            {
                CharNumber = Math.Min(state.CursorPosition.CharNumber + 1, currentLineLength),
                LineNumber = state.CursorPosition.LineNumber
            }
        };
    }
}