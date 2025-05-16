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

    private ViStack viStack;

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
        viStack = new();
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

      GD.Print("new line number", targetLineNumber);
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

    public void HandleKeyPress(InputKey key)
    {
      GD.Print("Code: ", key.KeyCode);
      GD.Print("Unicode: ", key.Unicode);
      GD.Print("IsShifted: ", key.IsShiftPressed);
      GD.Print("IsControlPressed: ", key.IsControlPressed);

      if (key.KeyCode == Key.Escape) {
        CurrentMode = ViMode.Normal;
        // clear action stack
        OnModeChange?.Invoke(this, EventArgs.Empty);
        viStack = new();
        return;
      }

      switch (CurrentMode) {
        case ViMode.Normal:
          HandleNormalMode(key);
          return;
        case ViMode.Insert:
          HandleInsertMode(key);
          return;
      }
    }

    private void HandleNormalMode(InputKey key)
    {
      var keyChar = key.ToString()[0];
      // Motion without an action
      var motion = MotionBuilder.HandleMotion(
        key,
        Lines,
        CursorPosition
      );

      // handle stack
      if (motion != null) {
        // motion is the last action so execute;
        if (
          viStack.CurrentAction == null
        ) {
          MoveCursorPosition(motion.End);
          return;
        } else {
          viStack.Motion = motion;

          var result = ActionBuilder.ExectueAction(
            CursorPosition,
            viStack,
            CurrentMode,
            Lines
          );
          HandleExecuteResult(result);
          return;
        }
        // else when there is an action
      }

      var action = ActionBuilder.GetAction(key, viStack);
      if (action.Action != null) {
        viStack.CurrentAction = action.Action;

        // add action to stack or execute it if ready
        if (action.IsReadyToExecute) {
          // ready to execute so do action
          var result = ActionBuilder.ExectueAction(
            CursorPosition,
            viStack,
            CurrentMode,
            Lines
          );
          HandleExecuteResult(result);
          return;
        }
        viStack.CurrentAction = action.Action;
        return;
      }

      // reset stack
      viStack = new();

      if (_motionMap.TryGetValue(key.KeyCode, out var v)) {
        MoveCursorPosition(new(){
          CharNumber = CursorPosition.CharNumber + v.CharNumber,
          LineNumber = CursorPosition.LineNumber + v.LineNumber
        });
      }
    }

    private void HandleInsertMode(InputKey key)
    {
      if (key.IsControlPressed && key.KeyCode == Key.V)
      {
        if (
          DisplayServer.ClipboardHasImage() ||
          !DisplayServer.ClipboardHas()
        ){
          return;
        }

        var textFromClipboard = DisplayServer.ClipboardGet();
        // TODO - move to end
        AddTextToCursor(textFromClipboard);
        return;
      }

      if (key.Unicode == null) return;
      var printableChar = Convert.ToChar(key.Unicode);
      AddCharToCursor(printableChar);
      MoveCursorPosition(new(){
          CharNumber = CursorPosition.CharNumber + 1,
          LineNumber = CursorPosition.LineNumber
      });
    }

    public void AddCharToCursor(char text)
    {
      Lines[CursorPosition.LineNumber] = Lines[CursorPosition.LineNumber]
        .Insert(CursorPosition.CharNumber, text.ToString());
      UpdateTextFromLinesBuffer();
    }

    public void AddTextToCursor(string text)
    {
      var linesToAdd = text.Split("\n");

      var firstLine = linesToAdd.FirstOrDefault();
      if (firstLine != null) {
        Lines[CursorPosition.LineNumber] = Lines[CursorPosition.LineNumber].Insert(CursorPosition.CharNumber, firstLine);
      }

      foreach (var line in linesToAdd.Skip(1))
      {
        Lines.Insert(CursorPosition.LineNumber + 1, line);
      }

      UpdateTextFromLinesBuffer();
    }

    public void UpdateTextFromLinesBuffer()
    {
      var newText = string.Join("\n", Lines);
      Content = newText;
      OnContentChanged?.Invoke(this, new EventArgs());
    }

    private void HandleExecuteResult(ExecuteResult result)
    {
      if (result.ChangedMode.HasValue)
      {
        CurrentMode = result.ChangedMode.Value;
        OnModeChange?.Invoke(this, EventArgs.Empty);
      }

      if (result.NewCursorPosition != null){
        CursorPosition = result.NewCursorPosition;
        OnCursorPositionChanged?.Invoke(this, EventArgs.Empty);
      }

      if (result.Lines != null) {
        Lines = result.Lines;
        UpdateTextFromLinesBuffer();
      }

      viStack = new();
    }
}