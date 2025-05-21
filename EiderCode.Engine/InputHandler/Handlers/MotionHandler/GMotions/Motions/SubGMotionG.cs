using EiderCode.Engine.Models;
using EiderCode.Engine;

public class SubGMotionG: IMotion
{
  // gg -> top of file char index 0
  public static Motion Handle(InputKey key, EngineState state)
  {
    return new(){
      Start = state.CursorPosition,
      End = new (0,0)
    };
  }
}