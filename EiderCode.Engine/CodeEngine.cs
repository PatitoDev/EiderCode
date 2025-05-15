using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using EiderCode.Engine.Models;
using EiderCode.Engine.TokenGeneration;


namespace EiderCode.Engine;


public class CodeEngine
{
    public event EventHandler? OnModeChange;
    public event EventHandler? OnContentChanged;
    public event EventHandler? OnCursorPositionChanged;

    private Tokenizer _tokenizer = new();

    public EditorPosition CursorPosition { get; private set; }
    public string Content { get; private set; }
    private List<string> Lines;
    public int LineCount { get; private set; }
    public string FilePath { get; private set; }

    public CodeEngine(string filePath, string content)
    {
        FilePath = filePath;
        Content = content;
        Lines = content.Split("\n").ToList();
        LineCount = Lines.Count();
        CursorPosition = new EditorPosition()
        {
            LineNumber = 0,
            CharNumber = 0
        };
    }

    public Document GetTokens()
    {
        var tokenizerResult = _tokenizer.Tokenize(FilePath, Content);

        if (tokenizerResult == null)
        {
            var lines = Lines
              .Select(contentLine => new DocumentLine()
              {
                  Tokens = new List<CodeToken>(){
                    new CodeToken {
                    Content = contentLine,
                    Scopes = Array.Empty<Scope>()
                  }
                }
              })
              .ToList();

            return new Document()
            {
                Lines = lines
            };
        }

        return new Document()
        {
            Lines = tokenizerResult.Select(line =>
              new DocumentLine()
              {
                  Tokens = line
              })
              .ToList()
        };
    }

    public void MoveCursorPosition(EditorPosition position)
    {
      var targetLineNumber = Math.Clamp(position.LineNumber, 0, Lines.Count - 1);
      var targetChar = Math.Clamp(position.CharNumber, 0,
        Math.Max(Lines[targetLineNumber]!.Length - 1, 0)
      );

      CursorPosition = new EditorPosition(){
        CharNumber = targetChar,
        LineNumber = targetLineNumber
      };
      OnCursorPositionChanged?.Invoke(this, new EventArgs());
    }

    private Dictionary<Key, EditorPosition> _motionMap = new(){
        { Key.J, new() { CharNumber = 0, LineNumber = 1 } },
        { Key.K, new() { CharNumber = 0, LineNumber = -1 } },
        { Key.L, new() { CharNumber = 1, LineNumber = 0 } },
        { Key.H, new() { CharNumber = -1, LineNumber = 0 } },
    };

    private Dictionary<Key, string> _insertMap = new(){
        { Key.Enter, "\\n" },
        { Key.Space, " " },
        { Key.Tab, "  " },
    };

    public ViMode CurrentMode = ViMode.Normal;

    public void HandleKeyPress(Key key)
    {
      if (key == Key.Escape) {
        CurrentMode = ViMode.Normal;
        return;
      }

      if (key == Key.I && CurrentMode == ViMode.Normal) {
        CurrentMode = ViMode.Insert;
        return;
      }

      if (CurrentMode == ViMode.Normal) {
        if (_motionMap.TryGetValue(key, out var v)) {
          MoveCursorPosition(new(){
            CharNumber = CursorPosition.CharNumber + v.CharNumber,
            LineNumber = CursorPosition.LineNumber + v.LineNumber
          });
        }
      }

      if (CurrentMode == ViMode.Insert) {
        GD.Print(key);
        GD.Print(OS.GetKeycodeString(key));

        MoveCursorPosition(new(){
            CharNumber = CursorPosition.CharNumber + 1,
            LineNumber = CursorPosition.LineNumber
        });
        AddTextToCursor(OS.GetKeycodeString(key));
      }
    }

    public void AddTextToCursor(string text)
    {
      Lines[CursorPosition.LineNumber] = Lines[CursorPosition.LineNumber].Insert(CursorPosition.CharNumber, text);
      UpdateTextFromLinesBuffer();
    }

    public void UpdateTextFromLinesBuffer()
    {
      var newText = string.Join("\n", Lines);
      Content = newText;
      OnContentChanged?.Invoke(this, new EventArgs());
    }
}