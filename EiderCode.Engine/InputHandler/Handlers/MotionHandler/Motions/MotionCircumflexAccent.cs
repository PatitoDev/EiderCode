using System;
using System.Collections.Generic;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionCircumflexAccent : IMotion
{
    // ^ - CIRCUMFLEX ACCENT
    // First non white character in line
    public static Motion? Handle(InputKey key, List<string> lines, EditorPosition cursorPosition)
    {
        var lineNumber = cursorPosition.LineNumber;
        var currentLine = lines[lineNumber]!;

        var index = Array.FindIndex(currentLine.ToCharArray(), (c) => !char.IsWhiteSpace(c));
        if (index == -1) return null;

        return new Motion()
        {
            Start = new()
            {
                CharNumber = cursorPosition.CharNumber,
                LineNumber = cursorPosition.LineNumber
            },
            End = new()
            {
                CharNumber = index,
                LineNumber = cursorPosition.LineNumber
            },
            MotionStack = key.KeyCode.ToString()
        };
    }
}