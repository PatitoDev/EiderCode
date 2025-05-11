using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

public record Scope
{
    public required string Name { get; init; }
    public required string FgColor { get; init; }
    public required string BgColor { get; init; }
}

public record CodeToken
{
    public required string Content { get; init; }
    public required IReadOnlyList<Scope> Scopes { get; init; }
}

public partial class CodeEditor : Control
{
    private TextEdit _textEditNode;
    private string _fileContent = "";
    private ScrollContainer _textContentNode;

    private Godot.Theme _theme;

    public override void _Ready()
    {
        _textEditNode = GetNode<TextEdit>("%EditorTextEdit");
        _textContentNode = GetNode<ScrollContainer>("%TextContent");
        _theme = GD.Load<Godot.Theme>("res://Theme/MainTheme.tres");
    }

    public void OpenFile(string filePath)
    {
        using var file = Godot.FileAccess.Open(
          filePath,
          Godot.FileAccess.ModeFlags.Read
        );
        var content = file.GetAsText();
        _textEditNode.Text = content;
        _fileContent = content;

        var tokens = Tokenize(filePath);
        GD.Print(tokens);
        Render(tokens);
    }

    public void Render(IReadOnlyList<IReadOnlyList<CodeToken>> tokens)
    {
        var children = _textContentNode.GetChildren();
        foreach (var child in children)
        {
            _textContentNode.RemoveChild(child);
        }

        var lines = new VBoxContainer();
        var lineIndex = 0;
        foreach (var lineTokens in tokens)
        {
            var line = new HBoxContainer();
            line.Theme = new Godot.Theme();
            line.AddThemeConstantOverride("separation", 0);

            var lineNumberNode = new Label();
            lineNumberNode.Text = lineIndex.ToString() + "   ";
            lineNumberNode.Theme = _theme;
            line.AddChild(lineNumberNode);

            foreach (var token in lineTokens)
            {
                var label = new Label();
                label.Text = token.Content;
                label.Theme = _theme;

                label.LabelSettings = new LabelSettings();

                foreach (var scope in token.Scopes)
                {
                    if (!string.IsNullOrEmpty(scope.FgColor))
                    {
                        label.LabelSettings.FontColor = Color.FromString(scope.FgColor, Colors.White);
                    }
                    if (!string.IsNullOrEmpty(scope.BgColor))
                    {
                        label.LabelSettings.OutlineColor = Color.FromString(scope.BgColor, Colors.White);
                        label.LabelSettings.OutlineSize = 5;
                    }
                }

                line.AddChild(label);
            }

            lines.AddChild(line);
            lineIndex += 1;
        }

        _textContentNode.AddChild(lines);
    }

    public IReadOnlyList<IReadOnlyList<CodeToken>> Tokenize(string filePath)
    {
        var options = new RegistryOptions(ThemeName.HighContrastDark);
        var registry = new Registry(options);
        var grammar = registry.LoadGrammar(options.GetScopeByExtension(Path.GetExtension(filePath)));
        GD.Print(grammar);
        var theme = registry.GetTheme();

        IStateStack? ruleStack = null;

        var fileLines = _fileContent.Split("\n");

        var parsedTokens = new List<List<CodeToken>>() { };

        foreach (var line in fileLines)
        {

            var lineTokens = new List<CodeToken>() { };

            var tokenizeResult = grammar.TokenizeLine(line, ruleStack, System.TimeSpan.MaxValue);
            ruleStack = tokenizeResult.RuleStack;

            foreach (var token in tokenizeResult.Tokens)
            {
                var content = line.Substr(token.StartIndex, token.Length);
                var themeRules = theme.Match(token.Scopes);

                var codeToken = new CodeToken()
                {
                    Content = content,
                    Scopes = themeRules.Select(rule => (
                      new Scope()
                      {
                          BgColor = theme.GetColor(rule.background),
                          FgColor = theme.GetColor(rule.foreground),
                          Name = rule.name
                      }
                    ))
                  .ToList()
                };

                lineTokens.Add(codeToken);
            }
            parsedTokens.Add(lineTokens);
        }

        return parsedTokens;
    }

}
