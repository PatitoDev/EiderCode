using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionDollarSign : IMotion
{

    // $ - Dollar Sign
    // last char in line
    public static Motion? Handle(InputKey key, EngineState state)
    {
        var lineNumber = state.CursorPosition.LineNumber;
        var currentLine = state.Lines[lineNumber]!;
        var lastChar = currentLine.Length - 1;

        return new Motion()
        {
            Start = new()
            {
                CharNumber = state.CursorPosition.CharNumber,
                LineNumber = state.CursorPosition.LineNumber
            },
            End = new()
            {
                CharNumber = lastChar,
                LineNumber = state.CursorPosition.LineNumber
            }
        };
    }
}