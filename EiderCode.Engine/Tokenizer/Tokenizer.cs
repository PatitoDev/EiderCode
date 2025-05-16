using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Internal.Themes.Reader;
using System.Text;

namespace EiderCode.Engine.TokenGeneration;


public class Tokenizer
{
    private Registry _registry;
    private RegistryOptions _options;
    public TextMateSharp.Themes.Theme Theme { get; private set; }

    public Tokenizer(){
        _options = new RegistryOptions(ThemeName.Dark);
        _registry = new Registry(_options);

        var draculaThemeJson = GD.Load<Json>("res://EiderCode.Engine/CodeEditorThemes/catppuccin_macchiato.json");
        var js = Json.Stringify(draculaThemeJson.Data);

        var bytes = Encoding.UTF8.GetBytes(js);
        var memoryStream = new MemoryStream(bytes);
        var streamReader = new StreamReader(memoryStream);

        var themeRaw = ThemeReader.ReadThemeSync(streamReader);
        _registry.SetTheme(themeRaw);
        Theme = _registry.GetTheme();
    }

    public IReadOnlyList<IReadOnlyList<CodeToken>>? Tokenize(
        string fileName,
        string fileContent
    )
    {
        var scopeByExtension = _options.GetScopeByExtension(Path.GetExtension(fileName));
        var grammar = _registry.LoadGrammar(scopeByExtension);
        if (grammar == null) return null;

        var theme = _registry.GetTheme();

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
                  .ToArray()
                };

                lineTokens.Add(codeToken);
            }
            parsedTokens.Add(lineTokens);
        }

        return parsedTokens;
    }
}
