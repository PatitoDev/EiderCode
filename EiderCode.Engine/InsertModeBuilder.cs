

using System;
using System.Collections.Generic;
using System.Linq;
using EiderCode.Engine.Models;
using Godot;


public record InsertResult
{
    public required List<string> Lines { get; init; }
    public required EditorPosition CursorPosition { get; init; }
}


public static class InsertModeBuilder
{

    public static InsertResult? HandleInsertMode(
      InputKey key,
      List<string> lines,
      EditorPosition cursorPosition
    )
    {
        if (key.IsControlPressed && key.KeyCode == Key.V)
        {
          return HandlePaste(key, lines, cursorPosition);
        }

        if (key.KeyCode == Key.Enter)
        {
          var currentLine = lines[cursorPosition.LineNumber];
          lines[cursorPosition.LineNumber] = currentLine.Substr(0, cursorPosition.CharNumber);
          var contentToMoveDown = currentLine.Substring(cursorPosition.CharNumber);

          lines.Insert(cursorPosition.LineNumber + 1, contentToMoveDown);
          return new(){
            CursorPosition = new(){
              CharNumber = 0,
              LineNumber = cursorPosition.LineNumber + 1
            },
            Lines = lines
          };
        }

        if (key.KeyCode == Key.Backspace)
        {
          var currentLine = lines[cursorPosition.LineNumber];
          if (cursorPosition.CharNumber == 0) {
            // at top of file
            if (cursorPosition.LineNumber == 0) return null;

            var previousLength = lines[cursorPosition.LineNumber - 1].Length;

            // at start of line
            lines.RemoveAt(cursorPosition.LineNumber);
            lines[cursorPosition.LineNumber - 1] += currentLine;

            return new() {
              Lines = lines,
              CursorPosition = new() {
                CharNumber = previousLength, // +1 to stay on the new char
                LineNumber = cursorPosition.LineNumber - 1
              }
            };
          }

          lines[cursorPosition.LineNumber] = lines[cursorPosition.LineNumber].Remove(cursorPosition.CharNumber - 1, 1);
          return new(){
            CursorPosition = new() {
              CharNumber = cursorPosition.CharNumber - 1,
              LineNumber = cursorPosition.LineNumber
            },
            Lines = lines,
          };
        }

        if (key.Unicode == null) return null;
        var printableChar = Convert.ToChar(key.Unicode);
        lines[cursorPosition.LineNumber] = lines[cursorPosition.LineNumber]
          .Insert(cursorPosition.CharNumber, printableChar.ToString());

        return new(){
          CursorPosition = new()
        {
            CharNumber = cursorPosition.CharNumber + 1,
            LineNumber = cursorPosition.LineNumber
        },
        Lines = lines
      };
    }

    private static InsertResult? HandlePaste(
      InputKey key,
      List<string> lines,
      EditorPosition cursorPosition
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

        var endChar = cursorPosition.CharNumber;
        var endLine = cursorPosition.LineNumber;

        var firstLine = linesToAdd.FirstOrDefault();
        if (firstLine != null)
        {
            var modifiedLine = lines[cursorPosition.LineNumber]
              .Insert(cursorPosition.CharNumber, firstLine);
            lines[cursorPosition.LineNumber] = modifiedLine;
            endChar = modifiedLine.Length;
        }

        foreach (var line in linesToAdd.Skip(1))
        {
            lines.Insert(cursorPosition.LineNumber + 1, line);
            endLine += 1;
            endChar = line.Length - 1;
        }

        return new InsertResult(){
          CursorPosition = new(){
            CharNumber = endChar,
            LineNumber = endLine,
          },
          Lines = lines
        };
    }
}