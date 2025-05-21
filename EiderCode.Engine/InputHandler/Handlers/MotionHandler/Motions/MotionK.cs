using System;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionK : IMotion
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
                CharNumber = state.CursorPosition.CharNumber,
                LineNumber = Math.Max(state.CursorPosition.LineNumber - 1, 0)
            }
        };
    }
}