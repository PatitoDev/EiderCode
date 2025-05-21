using System;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionCircumflexAccent : IMotion
{
    // ^ - CIRCUMFLEX ACCENT
    // First non white character in line
    public static Motion? Handle(InputKey key, EngineState state)
    {
        var lineNumber = state.CursorPosition.LineNumber;
        var currentLine = state.Lines[lineNumber]!;

        var index = Array.FindIndex(currentLine.ToCharArray(), (c) => !char.IsWhiteSpace(c));
        if (index == -1) return null;

        return new Motion()
        {
            Start = new()
            {
                CharNumber = state.CursorPosition.CharNumber,
                LineNumber = state.CursorPosition.LineNumber
            },
            End = new()
            {
                CharNumber = index,
                LineNumber = state.CursorPosition.LineNumber
            }
        };
    }
}