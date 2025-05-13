using Godot;
using System.Linq;

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
