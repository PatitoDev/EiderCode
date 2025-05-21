using System;
using System.Linq;
using EiderCode.Engine.Models;
using EiderCode.Engine;
using Godot;

public static class InsertModeHandler
{
    public static ExecuteResult? Handle(
      InputKey key,
      EngineState state
    )
    {
        switch(key)
        {
            case { IsControlPressed: true, KeyCode : Key.V }:
                return HandlePaste(key, state);

            case { KeyCode: Key.Enter }:
                return HandleEnter(state);

            case { KeyCode: Key.Backspace}:
                return HandleBackspace(state);

            case { KeyCode: Key.Tab }:
                return HandleTab(state);

            case { Unicode: null }:
                return null;

            default:
                return HandleUnicode(key, state);
        }
    }

    private static ExecuteResult HandleUnicode(InputKey key, EngineState state)
    {
        var printableChar = Convert.ToChar(key.Unicode);
        var lineNumber = state.CursorPosition.LineNumber;
        var charNumber = state.CursorPosition.CharNumber;
        var modifiedLines = state.Lines.ToList();

        modifiedLines[lineNumber] = modifiedLines[lineNumber]
            .Insert(charNumber, printableChar.ToString());

        var newCursorPosition = new EditorPosition(lineNumber, charNumber + 1);

        return new()
        {
            NewCursorPosition = newCursorPosition,
            Modification = new()
            {
                Lines = modifiedLines,
                StartPosition = newCursorPosition
            },
            ActionState = new()
        };
    }

    private static ExecuteResult HandleTab(EngineState state)
    {
        var lineNumber = state.CursorPosition.LineNumber;
        var charNumber = state.CursorPosition.CharNumber;
        var modifiedLines = state.Lines.ToList();

        // TODO - configure tab
        modifiedLines[lineNumber] = modifiedLines[lineNumber]
            .Insert(charNumber, "  ");

        var newCursorPosition = new EditorPosition(lineNumber, charNumber + 1);

        return new()
        {
            NewCursorPosition = newCursorPosition,
            Modification = new()
            {
                Lines = modifiedLines,
                StartPosition = newCursorPosition
            },
            ActionState = new()
        };
    }

    private static ExecuteResult? HandleBackspace(EngineState state)
    {
        var modifiedLines = state.Lines.ToList();

        var charNumber = state.CursorPosition.CharNumber;
        var lineNumber = state.CursorPosition.LineNumber;
        var currentLine = modifiedLines[lineNumber];

        // at top of file
        if (charNumber == 0)
        {
            if (lineNumber == 0) return null;

            var previousLineLength = modifiedLines[lineNumber - 1].Length;

            // move current line up to the previous line
            modifiedLines.RemoveAt(lineNumber);
            modifiedLines[lineNumber - 1] += currentLine;

            return new()
            {
                NewCursorPosition = new(
                    lineNumber - 1,
                    previousLineLength // +1 to stay on the new char (length instead of index)
                ),
                Modification = new()
                {
                    Lines = modifiedLines,
                    StartPosition = new(lineNumber - 1, 0)
                },
                ActionState = new()
            };
        }

        // remove 1 char
        modifiedLines[lineNumber] = modifiedLines[lineNumber].Remove(charNumber - 1, 1);

        return new()
        {
            NewCursorPosition = new(lineNumber, charNumber - 1),
            Modification = new()
            {
                Lines = modifiedLines,
                StartPosition = new(lineNumber, charNumber - 1)
            },
            ActionState = new()
        };

    }

    private static ExecuteResult HandleEnter(EngineState state)
    {
        var charNumber = state.CursorPosition.CharNumber;
        var lineNumber = state.CursorPosition.LineNumber;
        var modifiedLines = state.Lines.ToList();
        var currentLine = modifiedLines[state.CursorPosition.LineNumber];

        modifiedLines[lineNumber] = currentLine.Substr(0, charNumber);

        var contentToMoveDown = currentLine.Substring(charNumber);
        modifiedLines.Insert(lineNumber + 1, contentToMoveDown);

        return new()
        {
            NewCursorPosition = new(lineNumber + 1, 0),
            Modification = new()
            {
                Lines = modifiedLines,
                StartPosition = state.CursorPosition,
            },
            ActionState = new(),
        };
    }

    private static ExecuteResult? HandlePaste(
      InputKey key,
      EngineState state
    )
    {
        if (
          DisplayServer.ClipboardHasImage() ||
          !DisplayServer.ClipboardHas()
        )
        {
            return null;
        }

        var textFromClipboard = DisplayServer.ClipboardGet();
        // TODO - handle new lines correctly
        var linesToAdd = textFromClipboard.Split("/n").ToList();

        var endChar = state.CursorPosition.CharNumber;
        var endLine = state.CursorPosition.LineNumber;

        var firstLine = linesToAdd.FirstOrDefault();

        // copy lines to modify
        var linesModified = state.Lines.ToList();
        if (firstLine != null)
        {
            var modifiedLine = state
              .Lines[state.CursorPosition.LineNumber]
              .Insert(state.CursorPosition.CharNumber, firstLine);

            linesModified[state.CursorPosition.LineNumber] = modifiedLine;
            endChar = modifiedLine.Length;
        }

        foreach (var line in linesToAdd.Skip(1))
        {
            linesModified.Insert(state.CursorPosition.LineNumber + 1, line);
            endLine += 1;
            endChar = line.Length - 1;
        }

        return new()
        {
            NewCursorPosition = new()
            {
                CharNumber = endChar,
                LineNumber = endLine,
            },
            Modification = new()
            {
                Lines = linesModified,
                StartPosition = state.CursorPosition
            },
            ActionState = new()
        };
    }
}