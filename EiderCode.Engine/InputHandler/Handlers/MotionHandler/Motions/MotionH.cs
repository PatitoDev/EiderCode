using System;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionH : IMotion
{
    public static Motion? Handle(InputKey key, EngineState state)
    {
        return new Motion()
        {
            Start = new()
            {
                CharNumber = state.CursorPosition.CharNumber,
                LineNumber = state.CursorPosition.LineNumber,
            },
            End = new()
            {
                CharNumber = Math.Max(state.CursorPosition.CharNumber - 1, 0),
                LineNumber = state.CursorPosition.LineNumber
            }
        };
    }
}