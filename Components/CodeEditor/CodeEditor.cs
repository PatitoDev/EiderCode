using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;


public partial class CodeEditor : Control
{
    private Tokenizer _tokenizer = new();

    private string _fileContent = "";

    private TextEdit? _textEditNode;
    private VBoxContainer? _textContentNode;
    private VBoxContainer? _gutterContainer;
    private ScrollContainer? _scrollContainer;
    private Godot.Theme? _godotEditorTheme;

    private PackedScene _LineNumeberScene = GD.Load<PackedScene>("uid://bq36566tyvk1t");

    public override void _Ready()
    {
        _textEditNode = GetNode<TextEdit>("%EditorTextEdit");
        _textContentNode = GetNode<VBoxContainer>("%RowsContainer");
        _gutterContainer = GetNode<VBoxContainer>("%GutterContainer");
        _scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
        _godotEditorTheme = GD.Load<Godot.Theme>("res://Theme/CodeEditorTheme.tres");
        ClearEditor();
        RenderLineNumbers(0);
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
        _fileContent = content;

        var tokens = _tokenizer.Tokenize(filePath, content);
        if (tokens != null)
        {
            Render(tokens);
            return;
        }

        RenderFlat(content);
    }

    public void ClearEditor()
    {
        if (_textContentNode == null) return;

        var children = _textContentNode.GetChildren();
        foreach (var child in children)
        {
            _textContentNode.RemoveChild(child);
        }
    }

    public void RenderLineNumbers(int amount)
    {
        var MIN_LINES = 200;

        if (_gutterContainer == null) return;
        var children = _gutterContainer.GetChildren();

        var lineCount = 0;
        foreach (var child in children) {
            if (child is not LineNumber) continue;
            if (lineCount < amount){
                ((LineNumber)child).SetLineNumber(lineCount);
            } else {
                ((LineNumber)child).SetLineNumber();
            }

            lineCount += 1;
        }

        var totalLines = Math.Max(MIN_LINES, amount);
        if (lineCount >=  totalLines) return;

        var amountToCreate = (totalLines - lineCount) - 1;

        for (var i = 0; i < amountToCreate; i++){
            var lineNumber = _LineNumeberScene.Instantiate<LineNumber>();
            _gutterContainer.AddChild(lineNumber);

            if (lineCount < amount) {
                lineNumber.SetLineNumber(lineCount);
            } else {
                lineNumber.SetLineNumber();
            }

            lineCount += 1;
        }
    }

    public void RenderFlat(string content)
    {
        if (_textContentNode == null) return;
        ClearEditor();

        var lines = content.Split("/n");
        RenderLineNumbers(lines.Count());

        foreach (var line in lines){
            var lineContent = new Label();
            lineContent.Text = line;
            lineContent.Theme = _godotEditorTheme;
            _textContentNode.AddChild(lineContent);
        };
    }

    public void Render(IReadOnlyList<IReadOnlyList<CodeToken>> tokens)
    {
        if (
            _textContentNode == null ||
            _godotEditorTheme == null
        ) return;

        ClearEditor();
        RenderLineNumbers(tokens.Count());

        foreach (var lineTokens in tokens)
        {
            var line = new HBoxContainer();
            line.Theme = new Godot.Theme();
            line.AddThemeConstantOverride("separation", 0);

            foreach (var token in lineTokens)
            {
                var label = new Label();
                label.Text = token.Content;
                label.Theme = _godotEditorTheme;

                var scope = token.Scopes[0];
                if (!string.IsNullOrEmpty(scope.FgColor))
                {
                    label.AddThemeColorOverride("font_color", Color.FromString(scope.FgColor, Colors.Red));
                }
                if (!string.IsNullOrEmpty(scope.BgColor))
                {
                    label.AddThemeColorOverride("font_outline_color", Color.FromString(scope.BgColor, Colors.White));
                    label.AddThemeConstantOverride("outline_size", 10);
                }

                label.TooltipText = string.Join("\n",
                    token.Scopes.Select(s => s.Name).ToList()
                );
                label.MouseFilter = MouseFilterEnum.Pass;
                line.AddChild(label);
            }

            _textContentNode.AddChild(line);
        }
    }
}
