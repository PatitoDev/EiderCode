using System.Text.RegularExpressions;
using EiderCode.Engine;
using EiderCode.Engine.Models;

public class MotionE : IMotion
{

    // e
    // Go to the end of the next word
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

        var endIndex = firstMatch.Index + (firstMatch.Length - 1);
        var matchedLine = state.CursorPosition.LineNumber;
        var match = firstMatch;

        if (endIndex == 0)
        {
            // if we are at the end of the word already go the the next word
            // return as we can't find a secondary match
            if (matches.Count < 2 && (state.Lines.Count - 1) > lineNumber)
            {
                // if we don't have anything to match
                // try to match with line below
                var nextLineContent = state.Lines[lineNumber + 1]!;
                var nextLineMatch = Regex.Match(nextLineContent, "(\\w+)|(\\S)");
                if (nextLineMatch == null) return null; // nothing found
                match = nextLineMatch;
                matchedLine += 1;
            }
            else
            {
                // get next match
                match = matches[1];
            }
        }

        endIndex = match.Index + (match.Length - 1);
        if (state.CursorPosition.LineNumber == matchedLine)
        {
            // make sure to add substring offset
            endIndex += charPosition;
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
                CharNumber = endIndex,
                LineNumber = matchedLine
            }
        };
    }

}