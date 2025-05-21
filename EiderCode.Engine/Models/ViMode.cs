namespace EiderCode.Engine.Models;

public enum ViMode
{
  Normal,
  Insert,
  Visual
}

public enum SubMode
{
  Go, // g
  FindFordward, // f
  FindBackwards, // F
  TextObjectInside, // i
  TextObjectAround, // a
  TextObjectSurround, // s
  //TextObjectSelector, // i a s
  ReplaceChar, // r
  // z ?
}