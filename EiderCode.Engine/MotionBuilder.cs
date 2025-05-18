using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

  private static Dictionary<long,
    Func<
      InputKey,
      List<string>,
      EditorPosition, string , Motion?
    >
  > _funcMap = new(){
    { (long)Key.Key0 , Handle0 },
    { (long)Key.Asciicircum, HandleCircumflexAccent },
    { (long)Key.Dollar , HandleDollarSign },
    { (long)Convert.ToInt32('w') , HandleWord },
    { (long)Convert.ToInt32('b') , HandleBWord },
    { (long)Convert.ToInt32('j') , HandleJ },
    { (long)Convert.ToInt32('k') , HandleK },
    { (long)Convert.ToInt32('h') , HandleH },
    { (long)Convert.ToInt32('l') , HandleL },
    { (long)Convert.ToInt32('e') , HandleE },
  };

  public static Motion? HandleMotion(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition,
    string lastKey = ""
  )
  {
    if (!key.Unicode.HasValue) return null;

    if (!_funcMap.TryGetValue(key.Unicode.Value, out var method)) return null;
    return method(key, lines, cursorPosition, lastKey);
  }

  // $ - Dollar Sign
  // last char in line
  private static Motion? HandleDollarSign(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition,
    string lastKey = ""
  )
  {
    var lineNumber = cursorPosition.LineNumber;
    var currentLine = lines[lineNumber]!;
    var lastChar = currentLine.Length - 1;

    return new Motion(){
      Start = new() {
        CharNumber = cursorPosition.CharNumber,
        LineNumber = cursorPosition.LineNumber
      },
      End = new(){
        CharNumber = lastChar,
        LineNumber = cursorPosition.LineNumber
      },
      MotionStack = key.KeyCode.ToString()
    };
  }


  // ^ - CIRCUMFLEX ACCENT
  // First non white character in line
  private static Motion? HandleCircumflexAccent(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition,
    string lastKey = ""
  )
  {
    var lineNumber = cursorPosition.LineNumber;
    var currentLine = lines[lineNumber]!;

    var index = Array.FindIndex(currentLine.ToCharArray(), (c) => !char.IsWhiteSpace(c));
    if (index == -1) return null;

    return new Motion(){
      Start = new() {
        CharNumber = cursorPosition.CharNumber,
        LineNumber = cursorPosition.LineNumber
      },
      End = new(){
        CharNumber = index,
        LineNumber = cursorPosition.LineNumber
      },
      MotionStack = key.KeyCode.ToString()
    };
  }

  // 0
  // First character in line
  private static Motion? Handle0(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition,
    string lastKey = ""
  )
  {
    var lineNumber = cursorPosition.LineNumber;

    return new Motion(){
      Start = new() {
        CharNumber = cursorPosition.CharNumber,
        LineNumber = cursorPosition.LineNumber
      },
      End = new(){
        CharNumber = 0,
        LineNumber = cursorPosition.LineNumber
      },
      MotionStack = key.KeyCode.ToString()
    };
  }

  //h
  private static Motion? HandleH(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition,
    string lastKey = ""
  )
  {
    return new Motion(){
      Start = new(){
        CharNumber = cursorPosition.CharNumber,
        LineNumber = cursorPosition.LineNumber,
      },
      End = new(){
        CharNumber = Math.Max(cursorPosition.CharNumber - 1, 0),
        LineNumber = cursorPosition.LineNumber
      },
      MotionStack = key.KeyCode.ToString()
    };
  }

  //l
  private static Motion? HandleL(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition,
    string lastKey = ""
  )
  {
    var currentLineLength = lines[cursorPosition.LineNumber].Length;

    return new Motion(){
      Start = new(){
        CharNumber = cursorPosition.CharNumber,
        LineNumber = cursorPosition.LineNumber,
      },
      End = new(){
        CharNumber = Math.Min(cursorPosition.CharNumber + 1, currentLineLength),
        LineNumber = cursorPosition.LineNumber
      },
      MotionStack = key.KeyCode.ToString()
    };
  }

  //k
  private static Motion? HandleK(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition,
    string lastKey = ""
  )
  {
    return new Motion(){
      Start = new(){
        CharNumber = cursorPosition.CharNumber,
        LineNumber = cursorPosition.LineNumber,
      },
      End = new(){
        CharNumber = cursorPosition.CharNumber,
        LineNumber = Math.Max(cursorPosition.LineNumber - 1, 0)
      },
      MotionStack = key.KeyCode.ToString()
    };
  }

  //j
  private static Motion? HandleJ(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition,
    string lastKey = ""
  )
  {
    return new Motion(){
      Start = new(){
        CharNumber = cursorPosition.CharNumber,
        LineNumber = cursorPosition.LineNumber,
      },
      End = new(){
        CharNumber = cursorPosition.CharNumber,
        LineNumber = Math.Min(cursorPosition.LineNumber + 1, lines.Count)
      },
      MotionStack = key.KeyCode.ToString()
    };
  }

  // w
  // Go to the next word
  // word - characters, numberss or underscore sperated from whitespace
  private static Motion? HandleWord(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition,
    string lastKey = ""
  )
  {
    var charPosition = cursorPosition.CharNumber;
    var lineNumber = cursorPosition.LineNumber;
    var currentLine = lines[lineNumber]!;

    var stringToMatch = currentLine.Substring(charPosition);
    var matches = Regex.Matches(stringToMatch, "(\\w+)|(\\S)");

    if (matches.Count == 0) return null; // search on next line

    var firstMatch = matches[0];
    var isFirstMatchOnCursor = firstMatch.Index == 0;

    var index = firstMatch.Index;

    if (isFirstMatchOnCursor) {
      // return as we can't find a secondary match
      if (matches.Count < 2) return null; // search on next line

      // get next match
      var nextMatch = matches[1];
      index = nextMatch.Index;
    }

    return new Motion(){
      Start = new() {
        CharNumber = cursorPosition.CharNumber,
        LineNumber = cursorPosition.LineNumber
      },
      End = new(){
        CharNumber = charPosition + index,
        LineNumber = cursorPosition.LineNumber
      },
      MotionStack = key.KeyCode.ToString()
    };
  }

  // e
  // Go to the end of the next word
  // word - characters, numberss or underscore sperated from whitespace
  private static Motion? HandleE(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition,
    string lastKey = ""
  )
  {
    var charPosition = cursorPosition.CharNumber;
    var lineNumber = cursorPosition.LineNumber;
    var currentLine = lines[lineNumber]!;

    var stringToMatch = currentLine.Substring(charPosition);
    var matches = Regex.Matches(stringToMatch, "(\\w+)|(\\S)");

    if (matches.Count == 0) return null; // search on next line

    var firstMatch = matches[0];

    var endIndex = firstMatch.Index + (firstMatch.Length - 1);
    var matchedLine = cursorPosition.LineNumber;
    var match = firstMatch;

    if (endIndex == 0) {
      // if we are at the end of the word already go the the next word
      // return as we can't find a secondary match
      if (matches.Count < 2 && (lines.Count - 1) > lineNumber) {
        // if we don't have anything to match
        // try to match with line below
        var nextLineContent = lines[lineNumber + 1]!;
        var nextLineMatch = Regex.Match(nextLineContent, "(\\w+)|(\\S)");
        if (nextLineMatch == null) return null; // nothing found
        match = nextLineMatch;
        matchedLine += 1;
      } else {
        // get next match
        match = matches[1];
      }
    }

    endIndex = match.Index + (match.Length - 1);
    if (cursorPosition.LineNumber == matchedLine)
    {
      // make sure to add substring offset
      endIndex += charPosition;
    }

    return new Motion(){
      Start = new() {
        CharNumber = cursorPosition.CharNumber,
        LineNumber = cursorPosition.LineNumber
      },
      End = new(){
        CharNumber = endIndex,
        LineNumber = matchedLine
      },
      MotionStack = key.KeyCode.ToString()
    };
  }

  // b
  // Go to the previous word
  // word - characters, numberss or underscore sperated from whitespace
  private static Motion? HandleBWord(
    InputKey key,
    List<string> lines,
    EditorPosition cursorPosition,
    string lastKey = ""
  )
  {
    var charPosition = cursorPosition.CharNumber;
    var lineNumber = cursorPosition.LineNumber;
    var currentLine = lines[lineNumber]!;

    var stringToMatch = currentLine.Substring(0, charPosition + 1);
    var matches = Regex.Matches(stringToMatch, "(\\w+)|(\\S)");

    if (matches.Count == 0) return null; // search on next line

    // we are going backwards
    var firstMatch = matches[matches.Count - 1];
    var isFirstMatchOnCursor = firstMatch.Index == stringToMatch.Length - 1;

    var index = firstMatch.Index;

    if (isFirstMatchOnCursor) {
      // return as we can't find a secondary match
      if (matches.Count < 2) return null; // search on next line

      // get next match
      var nextMatch = matches[matches.Count - 2];
      index = nextMatch.Index;
    }

    return new Motion(){
      Start = new() {
        CharNumber = cursorPosition.CharNumber,
        LineNumber = cursorPosition.LineNumber
      },
      End = new(){
        CharNumber = index,
        LineNumber = cursorPosition.LineNumber
      },
      MotionStack = key.KeyCode.ToString()
    };
  }
}

