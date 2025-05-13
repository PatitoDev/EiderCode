using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;


public partial class CodeEditor : Control
{
    private Tokenizer _tokenizer = new();

    private TextEdit? _textEditNode;
    private VBoxContainer? _textContentNode;
    private VBoxContainer? _gutterContainer;
    private ScrollContainer? _scrollContainer;
    private Cursor? _cursor;
    private Godot.Theme? _godotEditorTheme;

    private PackedScene _LineNumeberScene = GD.Load<PackedScene>("uid://bq36566tyvk1t");
    private PackedScene _TokenLabelScene = GD.Load<PackedScene>("uid://c8twplky8hheg");

    private CodeEngine? _codeEngine;

    public override void _Ready()
    {
        _textEditNode = GetNode<TextEdit>("%EditorTextEdit");
        _textContentNode = GetNode<VBoxContainer>("%RowsContainer");
        _gutterContainer = GetNode<VBoxContainer>("%GutterContainer");
        _scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
        _cursor = GetNode<Cursor>("%Cursor");
        _godotEditorTheme = GD.Load<Godot.Theme>("res://Theme/CodeEditorTheme.tres");
        ClearEditor();
        RenderLineNumbers(0);
        _addCursorEvents();
    }

    public void OpenFile(string filePath)
    {
        if (
            _textEditNode == null ||
            _scrollContainer == null
        ) return;

        _scrollContainer.ScrollVertical = 0;

        using var file = Godot.FileAccess.Open(
          filePath,
          Godot.FileAccess.ModeFlags.Read
        );
        var content = file.GetAsText();
        _textEditNode.Text = content;

        _codeEngine = new CodeEngine(filePath, content);
        RenderCodeTokens(_codeEngine.GetTokens());
        _codeEngine.OnCursorPositionChanged += (o, e) => {
            var lineCount = _codeEngine.LineCount;
            RenderLineNumbers(lineCount);
            var newPostion = ConvertEditorPosition(_codeEngine.CursorPosition);
            if (newPostion != null) {
                _cursor?.MoveTo(newPostion.Value);
            }
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
            if (child is not LineNumber) continue;
            if (lineCount < amount)
            {
                var relNumber = Math.Abs(lineCount - cursorLinePosition);
                ((LineNumber)child).SetLineNumber(relNumber);
                ((LineNumber)child).SetIsCursorOnLine(relNumber == 0);
            }
            else
            {
                ((LineNumber)child).SetLineNumber();
                ((LineNumber)child).SetIsCursorOnLine(false);
            }

            lineCount += 1;
        }

        var totalLines = Math.Max(MIN_LINES, amount);
        if (lineCount >= totalLines) return;

        var amountToCreate = (totalLines - lineCount) - 1;

        for (var i = 0; i < amountToCreate; i++)
        {
            var lineNumber = _LineNumeberScene.Instantiate<LineNumber>();
            _gutterContainer.AddChild(lineNumber);

            if (lineCount < amount)
            {
                var relNumber = Math.Abs(lineCount - cursorLinePosition);
                lineNumber.SetLineNumber(relNumber);
                lineNumber.SetIsCursorOnLine(relNumber == 0);
            }
            else
            {
                lineNumber.SetLineNumber();
                lineNumber.SetIsCursorOnLine(false);
            }

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
                var label = _createLabel(token);
                label.StartChar = charCount;
                label.LineNumber = lineCount;
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

    private TokenLabel _createLabel(CodeToken token)
    {
        var label = _TokenLabelScene.Instantiate<TokenLabel>();
        label.SetToken(token);
        label.MouseFilter = MouseFilterEnum.Stop;

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

                var editorPosition = new EditorPosition(){
                    CharNumber = label.StartChar + i,
                    LineNumber = label.LineNumber,
                };

                _codeEngine.MoveCursorPosition(editorPosition);

                //_cursor.MoveTo(targetCursorPosition);
                _cursor.SetBlockSize(bounds.Size);
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
            var key = ((InputEventKey)@event).Keycode;
            _codeEngine?.HandleKeyPress(key);
        }
    }

    public Vector2? ConvertEditorPosition(EditorPosition position)
    {
        if (_textContentNode == null) return null;

        var row = _textContentNode.GetChild(position.LineNumber);
        var tokens = row.GetChildren();

        var charCount = 0;
        foreach (var token in tokens)
        {
            var tokenLabel = (TokenLabel)token;
            charCount += tokenLabel.StartChar;
            var contentLength = tokenLabel.Text.Length;

            if (
                position.CharNumber >= tokenLabel.StartChar &&
                position.CharNumber < tokenLabel.StartChar + contentLength
            ){
                GD.Print("found char in conversion: ", tokenLabel.Text);

                var bounds = tokenLabel.GetCharacterBounds(position.CharNumber - tokenLabel.StartChar);
                var targetCursorPosition = tokenLabel.GlobalPosition + bounds.Position;
                return targetCursorPosition;
            }
        }

        return null;
    }
}
