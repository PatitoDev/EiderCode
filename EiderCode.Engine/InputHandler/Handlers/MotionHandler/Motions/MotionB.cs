using System.Text.RegularExpressions;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionB : IMotion
{
    // b
    // Go to the previous word
    // word - characters, numberss or underscore sperated from whitespace
    public static Motion? Handle(InputKey key, EngineState state)
    {
        var charPosition = state.CursorPosition.CharNumber;
        var lineNumber = state.CursorPosition.LineNumber;
        var currentLine = state.Lines[lineNumber]!;

        var stringToMatch = currentLine.Substring(0, charPosition + 1);
        var matches = Regex.Matches(stringToMatch, "(\\w+)|(\\S)");

        if (matches.Count == 0) return null; // search on next line

        // we are going backwards
        var firstMatch = matches[matches.Count - 1];
        var isFirstMatchOnCursor = firstMatch.Index == stringToMatch.Length - 1;

        var index = firstMatch.Index;

        if (isFirstMatchOnCursor)
        {
            // return as we can't find a secondary match
            if (matches.Count < 2) return null; // search on next line

            // get next match
            var nextMatch = matches[matches.Count - 2];
            index = nextMatch.Index;
        }

        return new Motion()
        {
            Start = new()
            {
                CharNumber = charPosition,
                LineNumber = lineNumber
            },
            End = new()
            {
                CharNumber = index,
                LineNumber = lineNumber
            }
        };
    }
}