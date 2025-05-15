namespace EiderCode.Engine.TokenGeneration;


public record Scope
{
    public required string Name { get; init; }
    public required string FgColor { get; init; }
    public required string BgColor { get; init; }
}
