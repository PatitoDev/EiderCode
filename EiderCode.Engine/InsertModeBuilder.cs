

using System;
using System.Collections.Generic;
using System.Linq;
using EiderCode.Engine.Models;
using Godot;


public static class InsertModeBuilder
{

    public static ExecuteResult? HandleInsertMode(
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
            NewCursorPosition = new(){
              CharNumber = 0,
              LineNumber = cursorPosition.LineNumber + 1
            },
            Modification = new() {
              Lines = lines,
              StartPosition = cursorPosition,
            }
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
              NewCursorPosition = new() {
                CharNumber = previousLength, // +1 to stay on the new char
                LineNumber = cursorPosition.LineNumber - 1
              },
              Modification = new()
              {
                Lines = lines,
                StartPosition = new(0, cursorPosition.LineNumber - 1)
              }
            };
          }

          lines[cursorPosition.LineNumber] = lines[cursorPosition.LineNumber].Remove(cursorPosition.CharNumber - 1, 1);
          return new(){
            NewCursorPosition = new() {
              CharNumber = cursorPosition.CharNumber - 1,
              LineNumber = cursorPosition.LineNumber
            },
            Modification = new()
            {
              Lines = lines,
              StartPosition = new(cursorPosition.CharNumber - 1, cursorPosition.LineNumber)
            }
          };
        }

        if (key.Unicode == null) return null;
        var printableChar = Convert.ToChar(key.Unicode);
        lines[cursorPosition.LineNumber] = lines[cursorPosition.LineNumber]
          .Insert(cursorPosition.CharNumber, printableChar.ToString());

        var newCursorPosition = new EditorPosition(
          cursorPosition.CharNumber + 1,
          cursorPosition.LineNumber
        );

        return new(){
          NewCursorPosition = newCursorPosition,
          Modification = new(){
            Lines = lines,
            StartPosition = newCursorPosition
          }
        };
    }

    private static ExecuteResult? HandlePaste(
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

        return new(){
          NewCursorPosition = new(){
            CharNumber = endChar,
            LineNumber = endLine,
          },
          Modification = new() {
            Lines = lines,
            StartPosition = cursorPosition
          }
        };
    }
}