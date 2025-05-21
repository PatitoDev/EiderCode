using System.Collections.Generic;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionDollarSign : IMotion
{

    // $ - Dollar Sign
    // last char in line
    public static Motion? Handle(InputKey key, List<string> lines, EditorPosition cursorPosition)
    {
        var lineNumber = cursorPosition.LineNumber;
        var currentLine = lines[lineNumber]!;
        var lastChar = currentLine.Length - 1;

        return new Motion()
        {
            Start = new()
            {
                CharNumber = cursorPosition.CharNumber,
                LineNumber = cursorPosition.LineNumber
            },
            End = new()
            {
                CharNumber = lastChar,
                LineNumber = cursorPosition.LineNumber
            },
            MotionStack = key.KeyCode.ToString()
        };
    }
}