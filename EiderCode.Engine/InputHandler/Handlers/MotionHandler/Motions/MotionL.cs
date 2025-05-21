using System;
using System.Collections.Generic;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionL : IMotion
{
    public static Motion? Handle(InputKey key, List<string> lines, EditorPosition cursorPosition)
    {
        var currentLineLength = lines[cursorPosition.LineNumber].Length;

        return new Motion()
        {
            Start = new()
            {
                CharNumber = cursorPosition.CharNumber,
                LineNumber = cursorPosition.LineNumber,
            },
            End = new()
            {
                CharNumber = Math.Min(cursorPosition.CharNumber + 1, currentLineLength),
                LineNumber = cursorPosition.LineNumber
            },
            MotionStack = key.KeyCode.ToString()
        };
    }
}