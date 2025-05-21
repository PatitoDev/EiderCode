using System;
using System.Collections.Generic;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionJ : IMotion
{
    public static Motion? Handle(InputKey key, List<string> lines, EditorPosition cursorPosition)
    {
        return new Motion()
        {
            Start = new()
            {
                CharNumber = cursorPosition.CharNumber,
                LineNumber = cursorPosition.LineNumber,
            },
            End = new()
            {
                CharNumber = cursorPosition.CharNumber,
                LineNumber = Math.Min(cursorPosition.LineNumber + 1, lines.Count)
            },
            MotionStack = key.KeyCode.ToString()
        };
    }
}