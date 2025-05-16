using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Internal.Themes.Reader;
using System.Text;
using System.Threading.Tasks;
using EiderCode.Engine.Models;
using System.Threading;
using System;

namespace EiderCode.Engine.TokenGeneration;


public class Tokenizer
{
    private IGrammar? _grammar;

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

    public void LoadGrammar(string fileName)
    {
        var scopeByExtension = _options.GetScopeByExtension(Path.GetExtension(fileName));
        _grammar = _registry.LoadGrammar(scopeByExtension);
    }

    public (DocumentLine documentLine, IStateStack? ruleStack) TokenizeLine(
        string line,
        int index,
        IStateStack? stateStack = null
    )
    {
        if (_grammar == null) {
            // no syntax found so return line
            return (new DocumentLine() {
                Index = index,
                Tokens = [new CodeToken(){
                    Content = line,
                    Scopes = Array.Empty<Scope>()
                }]
            }, null);
        }
        var theme = _registry.GetTheme();
        var result = _grammar.TokenizeLine(line, stateStack, TimeSpan.MaxValue);

        var documentLine = new DocumentLine() {
            Index = index,
            Tokens = result
            .Tokens
            .Select(token => {
                var content = line.Substr(token.StartIndex, token.Length);
                // TODO - possible optimization here
                var themeRules = theme.Match(token.Scopes);

                return new CodeToken()
                {
                    Content = content,
                    Scopes = themeRules.Select(rule => (
                      new Scope()
                      {
                          BgColor = theme.GetColor(rule.background),
                          FgColor = theme.GetColor(rule.foreground),
                          Name = rule.name
                      })).ToArray()
                };
            })
            .ToArray()
        };

        return (documentLine, result.RuleStack);
    }

    public CodeToken[][]? Tokenize(
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

        var parsedTokens = new List<CodeToken[]>() { };

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
            parsedTokens.Add(lineTokens.ToArray());
        }

        return parsedTokens.ToArray();
    }
}
