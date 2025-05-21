using System;
using System.Collections.Generic;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionK : IMotion
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
                LineNumber = Math.Max(cursorPosition.LineNumber - 1, 0)
            },
            MotionStack = key.KeyCode.ToString()
        };
    }
}