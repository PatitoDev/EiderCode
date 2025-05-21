using System;
using System.Collections.Generic;
using EiderCode.Engine.Models;
using Godot;

namespace EiderCode.Engine;

/*
  hjkl -> movement 1 char


  // Left motions
  0 -> first character of the line
  ^ -> first non-blank character
  $ -> end of line
  g_ -> first non-blank character of the line counting downwards inclusive
  ??? -> last non-blank character of the line
  f{char} -> first char found in line inclusive
  F{char} -> first char found in line inclusive to the left
  t{char} -> first char found in line
  T{char} -> first char found in line to the left
  w or W -> to start of word exclusive
  e or E -> to end of word incluse
  b or B -> to start of word exclusive backwards
  ge or gE -> backwards to the end of the word inclusive

  // Up down motions
  - -> 1 line down
  + -> 1 line up
  gg -> go to line from top of the file to non-white char, no motion = 0
  G -> go to line from bottom of the file to non-white char, no motion = -1

  [count]% -> go to the percentage of the file //  ({count} * number-of-lines + 99) / 100

  ; -> repeat f, t, F, T, w
  , -> repeat f, t, F, T, w to the left
*/

public record Motion {
  public required string MotionStack { get; init; }
  public required EditorPosition Start { get; init; }
  public required EditorPosition End { get; init; }
}

public static class MotionBuilder
{

  private static Dictionary<
    long, Func<InputKey, List<string>, EditorPosition, Motion?>
  > _funcMap = new(){
    { (long)Key.Key0 , Motion0.Handle },
    { (long)Key.Asciicircum, MotionCircumflexAccent.Handle },
    { (long)Key.Dollar , MotionDollarSign.Handle },
    { (long)Convert.ToInt32('w') , MotionW.Handle },
    { (long)Convert.ToInt32('b') , MotionB.Handle },
    { (long)Convert.ToInt32('j') , MotionJ.Handle },
    { (long)Convert.ToInt32('k') , MotionK.Handle },
    { (long)Convert.ToInt32('h') , MotionH.Handle },
    { (long)Convert.ToInt32('l') , MotionL.Handle },
    { (long)Convert.ToInt32('e') , MotionE.Handle },
  };

  public static Motion? HandleMotion(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition
  )
  {
    if (!key.Unicode.HasValue) return null;

    if (!_funcMap.TryGetValue(key.Unicode.Value, out var method)) return null;
    return method(key, lines, cursorPosition);
  }
}
