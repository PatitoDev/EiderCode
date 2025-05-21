using System.Collections.Generic;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class Motion0 : IMotion
{
    // 0
    // First character in line
    public static Motion? Handle(InputKey key, List<string> lines, EditorPosition cursorPosition)
    {
        return new Motion()
        {
            Start = new()
            {
                CharNumber = cursorPosition.CharNumber,
                LineNumber = cursorPosition.LineNumber
            },
            End = new()
            {
                CharNumber = 0,
                LineNumber = cursorPosition.LineNumber
            },
            MotionStack = key.KeyCode.ToString()
        };
    }
}