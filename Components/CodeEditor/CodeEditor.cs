using Godot;
using System.Collections.Generic;


public partial class CodeEditor : Control
{
    private Tokenizer _tokenizer = new();

    private string _fileContent = "";

    private TextEdit? _textEditNode;
    private ScrollContainer? _textContentNode;
    private Godot.Theme? _godotEditorTheme;

    public override void _Ready()
    {
        _textEditNode = GetNode<TextEdit>("%EditorTextEdit");
        _textContentNode = GetNode<ScrollContainer>("%TextContent");
        _godotEditorTheme = GD.Load<Godot.Theme>("res://Theme/CodeEditorTheme.tres");
    }

    public void OpenFile(string filePath)
    {
        if (_textEditNode == null) return;

        using var file = Godot.FileAccess.Open(
          filePath,
          Godot.FileAccess.ModeFlags.Read
        );
        var content = file.GetAsText();
        _textEditNode.Text = content;
        _fileContent = content;

        var tokens = _tokenizer.Tokenize(filePath, content);
        if (tokens != null) {
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

    public void RenderFlat(string content)
    {
        if (_textContentNode == null) return;
        ClearEditor();

        var lines = content.Split("/n");

        var documentContainer = new VBoxContainer();
        var lineIndex = 0;

        foreach (var line in lines)
        {
            var lineContainer = new HBoxContainer();
            lineContainer.Theme = new Godot.Theme(); // todo - use existing theme
            lineContainer.AddThemeConstantOverride("separation", 0);

            var lineNumberNode = new Label();
            lineNumberNode.Text = lineIndex.ToString() + "   ";
            lineNumberNode.Theme = _godotEditorTheme;
            lineContainer.AddChild(lineNumberNode);

            var lineContent = new Label();
            lineContent.Text = line;
            lineContent.Theme = _godotEditorTheme;
            lineContainer.AddChild(lineContent);

            documentContainer.AddChild(lineContainer);

            lineIndex += 1;
        }
        _textContentNode.AddChild(documentContainer);
    }

    public void Render(IReadOnlyList<IReadOnlyList<CodeToken>> tokens)
    {
        if (
            _textContentNode == null ||
            _godotEditorTheme == null
        ) return;

        ClearEditor();

        var lines = new VBoxContainer();
        var lineIndex = 0;
        foreach (var lineTokens in tokens)
        {
            var line = new HBoxContainer();
            line.Theme = new Godot.Theme();
            line.AddThemeConstantOverride("separation", 0);

            var lineNumberNode = new Label();
            lineNumberNode.Text = lineIndex.ToString() + "   ";
            lineNumberNode.Theme = _godotEditorTheme;
            line.AddChild(lineNumberNode);

            foreach (var token in lineTokens)
            {
                var label = new Label();
                label.Text = token.Content;
                label.Theme = _godotEditorTheme;

                // TODO _ CHECKAL CJLAKJ SLDJLAS JL
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

                line.AddChild(label);
            }

            lines.AddChild(line);
            lineIndex += 1;
        }

        _textContentNode.AddChild(lines);
    }
}
