using System.Collections.Generic;
using EiderCode.Engine;
using EiderCode.Engine.Models;


interface IMotion {
  public static abstract Motion? Handle(InputKey key, List<string> lines, EditorPosition cursorPosition);
}
