using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using EiderCode.Engine.Models;
using EiderCode.Engine.TokenGeneration;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using TextMateSharp.Grammars;

namespace EiderCode.Engine;

public class OnLineParsedEventArgs : EventArgs
{
    public required DocumentLine Line { get; init; }
}


public class CodeEngine
{
    public event EventHandler? OnFinishedParsing;
    public event EventHandler<OnLineParsedEventArgs>? OnLineParsed;

    public event EventHandler? OnModeChange;
    public event EventHandler? OnContentChanged;
    public event EventHandler? OnContentChangedAndCursorMoved;
    public event EventHandler? OnCursorPositionChanged;

    private Tokenizer _tokenizer = new();

    public EditorPosition CursorPosition { get; private set; }
    public string Content { get; private set; } = "";
    public List<string> Lines = new List<string>();
    public List<DocumentLine> DocumentLines;
    public int LineCount { get; private set; } = 0;
    public string FilePath { get; private set; } = "";

    private ViStack viStack;

    public CodeEngine()
    {
        DocumentLines = new();
        CursorPosition = new EditorPosition()
        {
            LineNumber = 0,
            CharNumber = 0
        };
        viStack = new();
    }

    public Color GetGuiColor(string key)
    {
        var color = _tokenizer.Theme.GetGuiColorDictionary()[key];
        return Color.FromString(color, Colors.Red);
    }

    public void ClearOnLineParsedEvent()
    {
        if (OnLineParsed == null) return;

        foreach (Delegate d in OnLineParsed.GetInvocationList())
        {
            OnLineParsed -= (EventHandler<OnLineParsedEventArgs>)d;
        }
        GD.Print("clearing line: ", OnLineParsed?.GetInvocationList().Length);
    }

    public async Task<Document> OpenFileAsync(string filePath, CancellationToken cancellationToken)
    {
        Content = "";
        DocumentLines = new();
        viStack = new();
        CursorPosition = new EditorPosition()
        {
            LineNumber = 0,
            CharNumber = 0
        };
        Lines = new List<string>();
        FilePath = filePath;

        //Lines = content.Split("\n").ToList();
        //LineCount = Lines.Count()
        _tokenizer.LoadGrammar(filePath);
        var streamReader = File.OpenText(filePath);
        LineCount = 0;

        IStateStack? stack = null;

        while (!streamReader.EndOfStream)
        {
            var line = await streamReader.ReadLineAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) return new Document() { Lines = Array.Empty<DocumentLine>() };
            if (line == null) break; // handle done reading
            Content += line + "\n";
            Lines.Add(line);
            var (DocumentLine, newStack) = _tokenizer.TokenizeLine(line, LineCount, stack);
            if (cancellationToken.IsCancellationRequested) return new Document() { Lines = Array.Empty<DocumentLine>() };

            OnLineParsed?.Invoke(this, new()
            {
                Line = DocumentLine
            });

            LineCount += 1;
            stack = newStack;
            DocumentLines.Add(DocumentLine);
        }

        streamReader.Dispose();
        OnFinishedParsing?.Invoke(this, EventArgs.Empty);

        return new Document()
        {
            Lines = DocumentLines.ToArray()
        };

    }

    public Document GetTokens()
    {
        // should duplicate?
        return new Document(){
            Lines = DocumentLines.ToArray()
        };
        //_tokenizer.TokenizeDocument(FilePath, Content);
    }

    public void MoveCursorPosition(EditorPosition position)
    {
        var targetLineNumber = Math.Clamp(position.LineNumber, 0, Lines.Count - 1);
        var targetChar = Math.Clamp(position.CharNumber, 0,
          Math.Max(Lines[targetLineNumber]!.Length, 0)
        );

        CursorPosition = new EditorPosition()
        {
            CharNumber = targetChar,
            LineNumber = targetLineNumber
        };
        OnCursorPositionChanged?.Invoke(this, new EventArgs());
    }

    private Dictionary<Key, string> _insertMap = new(){
        { Key.Enter, "\\n" },
        { Key.Space, " " },
        { Key.Tab, "  " },
    };

    public ViMode CurrentMode = ViMode.Normal;

    public void HandleKeyPress(InputKey key)
    {
        /*
        GD.Print("Code: ", key.KeyCode);
        GD.Print("Unicode: ", key.Unicode);
        GD.Print("IsShifted: ", key.IsShiftPressed);
        GD.Print("IsControlPressed: ", key.IsControlPressed);
        */

        if (key.KeyCode == Key.Escape)
        {
            CurrentMode = ViMode.Normal;
            // clear action stacku
            OnModeChange?.Invoke(this, EventArgs.Empty);
            viStack = new();
            return;
        }

        switch (CurrentMode)
        {
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
        if (motion != null)
        {
            // motion is the last action so execute;
            if (
              viStack.CurrentAction == null
            )
            {
                MoveCursorPosition(motion.End);
                return;
            }
            else
            {
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
        if (action.Action != null)
        {
            viStack.CurrentAction = action.Action;

            // add action to stack or execute it if ready
            if (action.IsReadyToExecute)
            {
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
    }

    private void HandleInsertMode(InputKey key)
    {
        // TODO - use execute result model instead
        var result = InsertModeBuilder.HandleInsertMode(key, Lines, CursorPosition);
        if (result == null) return;
        HandleExecuteResult(result);
    }

    private void HandleExecuteResult(ExecuteResult result)
    {
        if (result.ChangedMode.HasValue)
        {
            CurrentMode = result.ChangedMode.Value;
            OnModeChange?.Invoke(this, EventArgs.Empty);
        }

        if (result.NewCursorPosition.HasValue && result.Modification == null)
        {
            // only send cursor update if there are no modifications
            CursorPosition = result.NewCursorPosition.Value;
            OnCursorPositionChanged?.Invoke(this, EventArgs.Empty);
        }

        if (result.Modification != null)
        {
            HandleModification(result.Modification);
            if (result.NewCursorPosition.HasValue) {
                CursorPosition = result.NewCursorPosition.Value;
                OnContentChangedAndCursorMoved?.Invoke(this, EventArgs.Empty);
            } else {
                OnContentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        viStack = new();
    }

    private void HandleModification(Modification modification){
        var newText = string.Join("\n", modification.Lines);
        Content = newText;
        Lines = modification.Lines;
        LineCount = modification.Lines.Count();
        var document = _tokenizer.TokenizeDocument(FilePath, Content);
        DocumentLines = document.Lines.ToList();
    }
}