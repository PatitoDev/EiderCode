using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public record DocumentLine
{
    public required IReadOnlyList<CodeToken> Tokens;
}

public record Document
{
    public required IReadOnlyList<DocumentLine> Lines;
}

public record EditorPosition
{
    public required int LineNumber { get; set; }
    public required int CharNumber { get; set; }
}


public class CodeEngine
{
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
                    Scopes = new List<Scope>(),
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
        Lines[targetLineNumber]!.Length - 1
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

    public void HandleKeyPress(Key key)
    {
      if (_motionMap.TryGetValue(key, out var v)) {
        MoveCursorPosition(new(){
          CharNumber = CursorPosition.CharNumber + v.CharNumber,
          LineNumber = CursorPosition.LineNumber + v.LineNumber
        });
      }
    }
}