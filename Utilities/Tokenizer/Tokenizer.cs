using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using TextMateSharp.Internal.Themes.Reader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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


public class Tokenizer
{
    public Tokenizer()
    {
    }

    public IReadOnlyList<IReadOnlyList<CodeToken>>? Tokenize(
        string filePath,
        string fileContent
    )
    {
        var options = new RegistryOptions(ThemeName.Dark);
        var registry = new Registry(options);
        var grammar = registry.LoadGrammar(options.GetScopeByExtension(Path.GetExtension(filePath)));

        if (grammar == null) return null;

        var draculaThemeJson = GD.Load<Json>("res://CodeEditorThemes/catppuccin_macchiato.json");
        var js = Json.Stringify(draculaThemeJson.Data);

        var bytes = Encoding.UTF8.GetBytes(js);
        var memoryStream = new MemoryStream(bytes);
        var streamReader = new StreamReader(memoryStream);

        var themeRaw = ThemeReader.ReadThemeSync(streamReader);
        registry.SetTheme(themeRaw);
        var theme = registry.GetTheme();

        IStateStack? ruleStack = null;

        var fileLines = fileContent.Split("\n");

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
