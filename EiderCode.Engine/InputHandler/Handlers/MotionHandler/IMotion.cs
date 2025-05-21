using EiderCode.Engine;
using EiderCode.Engine.Models;


interface IMotion {
  public static abstract Motion? Handle(
    InputKey key,
    EngineState state
  );
}
