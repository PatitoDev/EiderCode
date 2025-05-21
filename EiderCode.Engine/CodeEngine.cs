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
    public List<string> OriginalLines = new List<string>();
    public List<DocumentLine> DocumentLines;
    public int LineCount { get; private set; } = 0;
    public string FilePath { get; private set; } = "";
    public ViMode CurrentMode = ViMode.Normal;

    private ActionState ActionState;

    public CodeEngine()
    {
        DocumentLines = new();
        CursorPosition = new EditorPosition()
        {
            LineNumber = 0,
            CharNumber = 0
        };
        ActionState = new();
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
        ActionState = new();
        CursorPosition = new EditorPosition()
        {
            LineNumber = 0,
            CharNumber = 0
        };
        Lines = new List<string>();
        OriginalLines = new List<string>();
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
            OriginalLines.Add(line);
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
        return new Document()
        {
            Lines = DocumentLines.ToArray()
        };
    }

    private void SetCursorPosition(EditorPosition position)
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


    public void HandleKeyPress(InputKey key)
    {
        /*
        GD.Print("Code: ", key.KeyCode);
        GD.Print("Unicode: ", key.Unicode);
        GD.Print("IsShifted: ", key.IsShiftPressed);
        GD.Print("IsControlPressed: ", key.IsControlPressed);
        */
        var result = InputHandler.Handle(
            key,
            CurrentMode,
            Lines,
            CursorPosition,
            ActionState
        );

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
            if (result.NewCursorPosition.HasValue)
            {
                CursorPosition = result.NewCursorPosition.Value;
                OnContentChangedAndCursorMoved?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                OnContentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        ActionState = result.ActionState;
    }

    private void HandleModification(Modification modification)
    {
        var newText = string.Join("\n", modification.Lines);
        Content = newText;
        Lines = modification.Lines;
        LineCount = modification.Lines.Count();
        var document = _tokenizer.TokenizeDocument(FilePath, Content);

        var lineIndex = 0;
        var startIndexOfLinesToCompare = 0;

        foreach (var line in modification.Lines)
        {
            if (startIndexOfLinesToCompare >= OriginalLines.Count - 1) {
                document.Lines[lineIndex].Status = DocumentLineStatus.Modified;
            }

            var linesComparedCount = 0;
            foreach (var lineToCompare in OriginalLines.Skip(startIndexOfLinesToCompare))
            {
                var hasChanged = lineToCompare != line;

                if (!hasChanged) {
                    startIndexOfLinesToCompare += linesComparedCount + 1;
                    document.Lines[lineIndex].Status = DocumentLineStatus.UnModified;
                    break;
                }

                document.Lines[lineIndex].Status = DocumentLineStatus.Modified;
                linesComparedCount += 1;
            }

            lineIndex += 1;
        }

        DocumentLines = document.Lines.ToList();
    }
}