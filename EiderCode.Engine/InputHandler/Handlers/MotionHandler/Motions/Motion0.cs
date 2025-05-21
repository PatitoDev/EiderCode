using EiderCode.Engine;
using EiderCode.Engine.Models;

public class Motion0 : IMotion
{
    // 0
    // First character in line
    public static Motion? Handle(InputKey key, EngineState state)
    {
        return new Motion()
        {
            Start = new()
            {
                CharNumber = state.CursorPosition.CharNumber,
                LineNumber = state.CursorPosition.LineNumber
            },
            End = new()
            {
                CharNumber = 0,
                LineNumber = state.CursorPosition.LineNumber
            }
        };
    }
}