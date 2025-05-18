using Godot;

namespace EiderCode.Engine.TokenGeneration;

public record CodeToken
{
    public required string Content { get; init; }
    public required Color? FgColor { get; init; }
    public required Color? BgColor { get; init; }
    public required Scope[] Scopes { get; init; }
}