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

public record EngineState
{
    public required string Content { get; init; }
    public required IReadOnlyList<string> OriginalLines { get; init; }
    public required IReadOnlyList<string> Lines { get; init; }
    public required IReadOnlyList<DocumentLine> DocumentLines { get; init; }
    public required ViMode Mode { get; init; }
    public required SubMode? SubMode { get; init; }
    public required EditorPosition CursorPosition { get; init; }
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

    public string FilePath { get; private set; } = "";

    public EngineState State { get; private set; }
    private ActionState ActionState;

    public CodeEngine()
    {
        State = new() {
            Content = "",
            CursorPosition = new (0,0),
            DocumentLines =  new List<DocumentLine>(),
            Lines =  new List<string>(),
            Mode = ViMode.Normal,
            OriginalLines = new List<string>(),
            SubMode = null
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
        FilePath = filePath;
        State = new() {
            Content = "",
            CursorPosition = new (0,0),
            DocumentLines =  new List<DocumentLine>(),
            Lines =  new List<string>(),
            Mode = ViMode.Normal,
            OriginalLines = new List<string>(),
            SubMode = null
        };

        _tokenizer.LoadGrammar(filePath);
        var streamReader = File.OpenText(filePath);
        var lineCount = 0;

        IStateStack? stack = null;

        while (!streamReader.EndOfStream)
        {
            var line = await streamReader.ReadLineAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) return new Document() { Lines = Array.Empty<DocumentLine>() };
            if (line == null) break; // handle done reading

            var (documentLine, newStack) = _tokenizer.TokenizeLine(line, lineCount, stack);
            var content = State.Content + line + "\n";
            var lines = State.Lines.Append(line);
            var originalLines = State.OriginalLines.Append(line);
            var documentLines = State.DocumentLines.Append(documentLine);

            if (cancellationToken.IsCancellationRequested) return new Document() { Lines = Array.Empty<DocumentLine>() };

            State = State with {
                Content = content,
                Lines = lines.ToList(),
                OriginalLines = originalLines.ToList(),
                DocumentLines = documentLines.ToList(),
            };

            stack = newStack;
            OnLineParsed?.Invoke(this, new()
            {
                Line = documentLine
            });

            lineCount += 1;
        }

        streamReader.Dispose();
        OnFinishedParsing?.Invoke(this, EventArgs.Empty);

        return new Document()
        {
            Lines = State.DocumentLines.ToArray()
        };
    }

    public Document GetTokens()
    {
        return new Document()
        {
            Lines = State.DocumentLines.ToArray()
        };
    }

    private void SetCursorPosition(EditorPosition position)
    {
        var targetLineNumber = Math
            .Clamp(
                position.LineNumber,
                0,
                State.Lines.Count - 1
            );

        var targetChar = Math
            .Clamp(
                position.CharNumber,
                0,
                Math.Max(State.Lines[targetLineNumber]!.Length, 0)
            );

        State = State with { CursorPosition = new EditorPosition()
        {
            CharNumber = targetChar,
            LineNumber = targetLineNumber
        }};
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
            State,
            ActionState
        );

        if (result == null) return;
        HandleExecuteResult(result);
    }

    private void HandleExecuteResult(ExecuteResult result)
    {
        if (result.ChangedMode.HasValue)
        {
            State = State with {
                Mode = result.ChangedMode.Value,
            };
            OnModeChange?.Invoke(this, EventArgs.Empty);
        }

        State = State with {
            SubMode = result.ChangedSubMode
        };

        if (result.NewCursorPosition.HasValue && result.Modification == null)
        {
            // only send cursor update if there are no modifications
            State = State with {
                CursorPosition = result.NewCursorPosition.Value
            };
            OnCursorPositionChanged?.Invoke(this, EventArgs.Empty);
        }

        if (result.Modification != null)
        {
            HandleModification(result.Modification);
            if (result.NewCursorPosition.HasValue)
            {
                State = State with {
                    CursorPosition = result.NewCursorPosition.Value
                };
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
        var document = _tokenizer.TokenizeDocument(FilePath, newText);

        var lineIndex = 0;
        var startIndexOfLinesToCompare = 0;

        foreach (var line in modification.Lines)
        {
            if (startIndexOfLinesToCompare >= State.OriginalLines.Count - 1) {
                document.Lines[lineIndex].Status = DocumentLineStatus.Modified;
            }

            var linesComparedCount = 0;
            foreach (var lineToCompare in State.OriginalLines.Skip(startIndexOfLinesToCompare))
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

        State = State with {
            DocumentLines = document.Lines.ToList(),
            Content = newText,
            Lines = modification.Lines,
        };
    }
}