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

    public Dictionary<string, Color> _colorMap = new();

    public Tokenizer(){
        _options = new RegistryOptions(ThemeName.Dark);
        _registry = new Registry(_options);

        var draculaThemeJson = GD.Load<Json>("res://EiderCode.Engine/CodeEditorThemes/catppuccin_mocha.json");
        var js = Json.Stringify(draculaThemeJson.Data);

        var bytes = Encoding.UTF8.GetBytes(js);
        var memoryStream = new MemoryStream(bytes);
        var streamReader = new StreamReader(memoryStream);

        var themeRaw = ThemeReader.ReadThemeSync(streamReader);
        _registry.SetTheme(themeRaw);
        Theme = _registry.GetTheme();
    }

    public Color? GetColorFromStringInCache(int? scopeId)
    {
        if (!scopeId.HasValue) return null;

        var colorString = Theme.GetColor(scopeId.Value);
        if (string.IsNullOrEmpty(colorString)) return null;

        if (_colorMap.TryGetValue(colorString, out var colorFromCache)){
            return colorFromCache;
        }

        var color = Color.FromString(colorString, Colors.White);
        _colorMap[colorString] = color;
        return color;
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
                    FgColor = Colors.White, // TODO - set default colot from theme
                    BgColor = Colors.White, // TODO - set default colot from theme
                    Scopes = Array.Empty<Scope>()
                }]
            }, null);
        }
        var result = _grammar.TokenizeLine(line, stateStack, TimeSpan.MaxValue);

        var documentLine = new DocumentLine() {
            Index = index,
            Tokens = result
            .Tokens
            .Select(token => {
                var content = line.Substr(token.StartIndex, token.Length);
                // TODO - possible optimization here
                var themeRules = Theme.Match(token.Scopes);
                var firstRule = themeRules.FirstOrDefault();

                return new CodeToken()
                {
                    Content = content,
                    FgColor = GetColorFromStringInCache(firstRule?.foreground),
                    BgColor = GetColorFromStringInCache(firstRule?.background),
                    Scopes = themeRules.Select(rule => (
                      new Scope()
                      {
                          BgColor = Theme.GetColor(rule.background),
                          FgColor = Theme.GetColor(rule.foreground),
                          Name = rule.name
                      })).ToArray()
                };
            })
            .ToArray()
        };

        return (documentLine, result.RuleStack);
    }

    public Document TokenizeDocument(string fileName, string fileContent)
    {
        if (_grammar == null) {
            // no syntax found so return document with no syntax
            return new() {
                Lines = Array.Empty<DocumentLine>()
            };
        }

        IStateStack? stack = null;

        var documentLines = new List<DocumentLine>();
        var lines = fileContent.Split("\n");
        var lineIndex = 0;

        foreach (var line in lines)
        {
            var result = TokenizeLine(line, lineIndex, stack);
            stack = result.ruleStack;
            documentLines.Add(result.documentLine);
            lineIndex += 1;
        }

        return new(){
            Lines = documentLines.ToArray()
        };
    }
}
