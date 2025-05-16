namespace EiderCode.Engine.Models;

public record EditorPosition
{
    public EditorPosition(int lineNumber, int charNumber){
        LineNumber = lineNumber;
        CharNumber = charNumber;
    }

    public EditorPosition() {}

    public int LineNumber { get; init; }
    public int CharNumber { get; init; }
}