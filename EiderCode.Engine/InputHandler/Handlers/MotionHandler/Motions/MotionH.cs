using System;
using System.Collections.Generic;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionH : IMotion
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
                CharNumber = Math.Max(cursorPosition.CharNumber - 1, 0),
                LineNumber = cursorPosition.LineNumber
            },
            MotionStack = key.KeyCode.ToString()
        };
    }
}