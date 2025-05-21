using System;
using System.Linq;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionShiftG : IMotion
{
    // shift + g - go to bottom of file and last char
    public static Motion? Handle(InputKey key, EngineState state)
    {
        var lastLineIndex = state.Lines.Count - 1;
        var lastCharIndex = state.Lines.LastOrDefault()?.Count() ?? 0;

        return new()
        {
            Start = state.CursorPosition,
            End = new(lastLineIndex, lastCharIndex)
        };
    }
}