namespace EiderCode.Engine.Models;

public record EditorPosition
{
    public required int LineNumber { get; set; }
    public required int CharNumber { get; set; }
}