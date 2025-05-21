using System.Collections.Generic;
using System.Text.RegularExpressions;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionW : IMotion
{
    // w
    // Go to the next word
    // word - characters, numberss or underscore sperated from whitespace
    public static Motion? Handle(InputKey key, List<string> lines, EditorPosition cursorPosition)
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

        if (isFirstMatchOnCursor)
        {
            // return as we can't find a secondary match
            if (matches.Count < 2) return null; // search on next line

            // get next match
            var nextMatch = matches[1];
            index = nextMatch.Index;
        }

        return new Motion()
        {
            Start = new()
            {
                CharNumber = cursorPosition.CharNumber,
                LineNumber = cursorPosition.LineNumber
            },
            End = new()
            {
                CharNumber = charPosition + index,
                LineNumber = cursorPosition.LineNumber
            },
            MotionStack = key.KeyCode.ToString()
        };
    }
}