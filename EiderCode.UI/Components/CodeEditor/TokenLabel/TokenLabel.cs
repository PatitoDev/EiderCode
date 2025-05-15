using EiderCode.Engine.TokenGeneration;
using Godot;
using System.Linq;
using static Godot.Control;

namespace EiderCode.UI;

public partial class TokenLabel : Label
{
    public CodeToken? Token;
    public int LineNumber;
    public int StartChar;

    public void SetToken(CodeToken token)
    {
        Text = token.Content;
        Token = token;

        var scope = token.Scopes.FirstOrDefault();

        if (scope != null && !string.IsNullOrEmpty(scope.FgColor))
        {
            AddThemeColorOverride("font_color", Color.FromString(scope.FgColor, Colors.Red));
        }
        if (scope != null && !string.IsNullOrEmpty(scope.BgColor))
        {
            AddThemeColorOverride("font_outline_color", Color.FromString(scope.BgColor, Colors.White));
            AddThemeConstantOverride("outline_size", 10);
        }

        TooltipText = string.Join("\n",
            token.Scopes.Select(s => s.Name).ToList()
        );
        MouseFilter = MouseFilterEnum.Stop;
    }
};


public static class TokenLabelBuilder
{
    public enum TokenLabelMetadataKeys {
        LineNumber,
        StartChar,
    }

    public static int GetLineNumber(Label label){
        return label.GetMeta(TokenLabelMetadataKeys.LineNumber.ToString()).AsInt32();
    }

    public static int GetStartChar(Label label){
        return label.GetMeta(TokenLabelMetadataKeys.StartChar.ToString()).AsInt32();
    }

    public static Label Create(CodeToken token, Theme theme, int lineNumber, int startChar)
    {
        var label = new Label();
        label.Text = token.Content;
        label.Theme = theme;
        label.SetMeta(TokenLabelMetadataKeys.LineNumber.ToString(), lineNumber);
        label.SetMeta(TokenLabelMetadataKeys.StartChar.ToString(), startChar);
        //Token = token;

        var scope = token.Scopes.FirstOrDefault();

        if (scope != null && !string.IsNullOrEmpty(scope.FgColor))
        {
            label.AddThemeColorOverride("font_color", Color.FromString(scope.FgColor, Colors.Red));
        }
        if (scope != null && !string.IsNullOrEmpty(scope.BgColor))
        {
            label.AddThemeColorOverride("font_outline_color", Color.FromString(scope.BgColor, Colors.White));
            label.AddThemeConstantOverride("outline_size", 10);
        }

        label.TooltipText = string.Join("\n",
            token.Scopes.Select(s => s.Name).ToList()
        );
        label.MouseFilter = MouseFilterEnum.Stop;

        return label;
    }
}