using EiderCode.Engine.Models;
using EiderCode.Engine;
using System.Collections.Generic;
using System;


public static class SubGMotion
{
  private static Dictionary<long, Func<InputKey, EngineState, Motion?>> _handlerMap = new(){
    { (long)Convert.ToInt32('g') , SubGMotionG.Handle },
  };

  public static Motion? Handle(InputKey key, EngineState state)
  {
    if (!key.Unicode.HasValue) return null;

    if (!_handlerMap.TryGetValue(key.Unicode.Value, out var method)) return null;
    return method(key, state);
  }
}