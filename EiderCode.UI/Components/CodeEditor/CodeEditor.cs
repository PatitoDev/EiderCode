using EiderCode.Engine.Models;
using EiderCode.Engine;
using EiderCode.Engine.TokenGeneration;
using Godot;
using System;
using System.Diagnostics;
using System.Linq;

namespace EiderCode.UI;

public partial class CodeEditor : Control
{
    private Tokenizer _tokenizer = new();

    private VBoxContainer? _textContentNode;
    private VBoxContainer? _gutterContainer;
    private ScrollContainer? _scrollContainer;
    private Cursor? _cursor;
    private Godot.Theme? _godotEditorTheme;

    private PackedScene _LineNumeberScene = GD.Load<PackedScene>("uid://bq36566tyvk1t");
    private PackedScene _TokenLabelScene = GD.Load<PackedScene>("uid://buyrejx7ssqx7");
    private PackedScene _TokenLabelSceneGD = GD.Load<PackedScene>("uid://bpypqmtabddwg");

    private CodeEngine? _codeEngine;

    public override void _Ready()
    {
        _textContentNode = GetNode<VBoxContainer>("%RowsContainer");
        _gutterContainer = GetNode<VBoxContainer>("%GutterContainer");
        _scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
        _cursor = GetNode<Cursor>("%Cursor");
        _godotEditorTheme = GD.Load<Godot.Theme>("res://EiderCode.UI/Theme/CodeEditorTheme.tres");
        ClearEditor();
        RenderLineNumbers(0);
        _addCursorEvents();
    }

    public void OpenFile(string filePath)
    {
        if (_scrollContainer == null) return;

        _scrollContainer.ScrollVertical = 0;

        var s = new Stopwatch();
        s.Start();
        using var file = Godot.FileAccess.Open(
          filePath,
          Godot.FileAccess.ModeFlags.Read
        );
        var content = file.GetAsText();

        _codeEngine = new CodeEngine(filePath, content);
        RenderCodeTokens(_codeEngine.GetTokens());
        s.Stop();
        GD.Print("Loaded and rendered file in: ", s.ElapsedMilliseconds);

        _codeEngine.OnContentChanged += (o, e) => {
            var s2 = new Stopwatch();
            s2.Start();
            RenderCodeTokens(_codeEngine.GetTokens());
            s2.Stop();
            GD.Print("Reloaded file in: ", s.ElapsedMilliseconds);
        };

        _codeEngine.OnCursorPositionChanged += (o, e) => {
          CallDeferred(CodeEditor.MethodName.UpdateCursorPosition);
          //  UpdateCursorPosition();
        };

        _codeEngine.OnModeChange += (o, e) => {
            if (_cursor == null) return;

            _cursor.SetCursorType(
                _codeEngine.CurrentMode == ViMode.Insert ?
                 CursorType.Line :
                 CursorType.Block
            );
        };
    }

    public void ClearEditor()
    {
        if (_textContentNode == null) return;

        var children = _textContentNode.GetChildren();
        foreach (var child in children)
        {
            _textContentNode.RemoveChild(child);
            child.QueueFree();
        }
    }

    public void RenderLineNumbers(int amount)
    {
        var MIN_LINES = 200;

        var cursorLinePosition = _codeEngine?.CursorPosition.LineNumber ?? 0;

        if (_gutterContainer == null) return;
        var children = _gutterContainer.GetChildren();
        var lineCount = 0;
        foreach (var child in children)
        {
            if (child is not MarginContainer) continue;
            if (lineCount < amount)
            {
                var relNumber = Math.Abs(lineCount - cursorLinePosition);
                LineNumberBuilder.UpdateNumber((MarginContainer) child, relNumber, relNumber == 0);
                //((LineNumber)child).SetLineNumber(relNumber);
                //((LineNumber)child).SetIsCursorOnLine(relNumber == 0);
            }
            else
            {
                LineNumberBuilder.UpdateNumber((MarginContainer) child, null, false);
                //((LineNumber)child).SetLineNumber();
                //((LineNumber)child).SetIsCursorOnLine(false);
            }

            lineCount += 1;
        }

        var totalLines = Math.Max(MIN_LINES, amount);
        if (lineCount >= totalLines) return;

        var amountToCreate = (totalLines - lineCount) - 1;

        for (var i = 0; i < amountToCreate; i++)
        {
            //var lineNumber = _LineNumeberScene.Instantiate<LineNumber>();

            int? val = null;
            if (lineCount < amount)
            {
                var relNumber = Math.Abs(lineCount - cursorLinePosition);
                //lineNumber.SetLineNumber(relNumber);
                //lineNumber.SetIsCursorOnLine(relNumber == 0);
                val = relNumber;
            }
            else
            {
                //lineNumber.SetLineNumber();
                //lineNumber.SetIsCursorOnLine(false);
            }
            var lineNumber = LineNumberBuilder.Create(val, _godotEditorTheme!, val == 0);
            _gutterContainer.AddChild(lineNumber);

            lineCount += 1;
        }
    }

    public void RenderCodeTokens(Document document)
    {
        if (
            _textContentNode == null ||
            _godotEditorTheme == null
        ) return;

        ClearEditor();
        RenderLineNumbers(document.Lines.Count());

        var lineCount = 0;

        foreach (var line in document.Lines)
        {
            var lineContainer = new HBoxContainer();
            lineContainer.Theme = new Godot.Theme();
            lineContainer.AddThemeConstantOverride("separation", 0);

            var charCount = 0;

            foreach (var token in line.Tokens)
            {
                var label = _createLabel(token, lineCount, charCount);
                //label.StartChar = charCount;
                //label.LineNumber = lineCount;
                lineContainer.AddChild(label);
                charCount += token.Content.Length;
            }

            _textContentNode.AddChild(lineContainer);
            lineCount += 1;
        }
    }

    private void _addCursorEvents(){
        if (_scrollContainer == null) return;
        _scrollContainer.GuiInput += (e) => {
            if (e is not InputEventMouseButton) return;
            var mouseEvent = (InputEventMouseButton) e;
            if (mouseEvent.ButtonIndex != MouseButton.Left) return;
            GD.Print("Clicked on scroll");
            // not implemented
        };
    }

    private Node _createLabel(CodeToken token, int lineNumber, int startChar)
    {
        var label = TokenLabelBuilder.Create(token, _godotEditorTheme!, lineNumber, startChar);
        //var label = _TokenLabelScene.Instantiate<TokenLabel>();
        //var label = new Label();
        //label.Theme = _godotEditorTheme;
        //var label = (Label)_TokenLabelSceneGD.Instantiate();
        //label.Call("setLabel", token.Content);
        //label.SetToken(token);
        //label.Text = token.Content;

        label.MouseFilter = MouseFilterEnum.Pass;

        label.GuiInput += (e) => {
            if (e is not InputEventMouseButton) return;
            var mouseEvent = (InputEventMouseButton) e;
            if (mouseEvent.ButtonIndex != MouseButton.Left) return;

            var localPosition = mouseEvent.Position;
            var charCount = label.Text.Length;

            GD.Print("Clicked on label: ", label.Text);
            for (var i = 0; i < charCount; i++) {
                var bounds = label.GetCharacterBounds(i);

                if (!(localPosition.X > bounds.Position.X && localPosition.X < bounds.End.X)) continue;

                GD.Print("Character clicked: ", label.Text[i]);

                var targetCursorPosition = label.GlobalPosition + bounds.Position;
                if (_cursor == null || _codeEngine == null) return;

                var startChar = TokenLabelBuilder.GetStartChar(label);
                var lineNumber = TokenLabelBuilder.GetLineNumber(label);
                var editorPosition = new EditorPosition(){
                    CharNumber = startChar + i,
                    LineNumber = lineNumber,
                };

                _codeEngine.MoveCursorPosition(editorPosition);

                //_cursor.MoveTo(targetCursorPosition);
                //_cursor.SetBlockSize(bounds.Size);
                return;
            }
        };
        return label;
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);

        if (
            @event is InputEventKey &&
            ((InputEventKey)@event).IsPressed()
        )
        {
            var inputEventKey = ((InputEventKey)@event);

            _codeEngine?.HandleKeyPress(new(){
                IsShiftPressed = inputEventKey.ShiftPressed,
                IsControlPressed = inputEventKey.CtrlPressed,
                KeyCode = inputEventKey.PhysicalKeycode,
                Unicode = inputEventKey.Unicode == 0 ? null : inputEventKey.Unicode
            });
        }
    }

    public void UpdateCursorPosition()
    {
        if (_codeEngine == null) return;

        var lineCount = _codeEngine.LineCount;
        RenderLineNumbers(lineCount);

        var newPostion = ConvertEditorPosition(_codeEngine.CursorPosition);
        if (newPostion != null) {
            _cursor?.MoveTo(newPostion.Value.position);
            _cursor?.SetChar(newPostion.Value.character);
        }

    }

    public (Vector2 position, char character)? ConvertEditorPosition(EditorPosition position)
    {
        if (_textContentNode == null) return null;

        var row = _textContentNode.GetChild<HBoxContainer>(position.LineNumber);
        var tokens = row.GetChildren();

        var charCount = 0;
        foreach (var token in tokens)
        {
            var tokenLabel = (Label)token;
            //var startChar = TokenLabelBuilder.GetStartChar(tokenLabel);
            //charCount += tokenLabel.Text.Length;
            var contentLength = tokenLabel.Text.Length;

            if (
                position.CharNumber >= charCount &&
                position.CharNumber < charCount + contentLength
            ){
                GD.Print("found char in conversion: ", tokenLabel.Text);

                var bounds = tokenLabel.GetCharacterBounds(position.CharNumber - charCount);
                var targetCursorPosition = tokenLabel.GlobalPosition + bounds.Position;
                return (targetCursorPosition, tokenLabel.Text[position.CharNumber - charCount]);
            }
            charCount += tokenLabel.Text.Length;
        }

        // TODO - approximate position
        // use text server to get char position
        return null;
    }
}
