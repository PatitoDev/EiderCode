using System.Text.RegularExpressions;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionW : IMotion
{
    // w
    // Go to the next word
    // word - characters, numberss or underscore sperated from whitespace
    public static Motion? Handle(InputKey key, EngineState state)
    {
        var charPosition = state.CursorPosition.CharNumber;
        var lineNumber = state.CursorPosition.LineNumber;
        var currentLine = state.Lines[lineNumber]!;

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
                CharNumber = state.CursorPosition.CharNumber,
                LineNumber = state.CursorPosition.LineNumber
            },
            End = new()
            {
                CharNumber = charPosition + index,
                LineNumber = state.CursorPosition.LineNumber
            }
        };
    }
}