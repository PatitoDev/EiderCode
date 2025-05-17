namespace EiderCode.Engine.Models;

public readonly struct EditorPosition
{
    public EditorPosition(int lineNumber, int charNumber){
        LineNumber = lineNumber;
        CharNumber = charNumber;
    }

    public EditorPosition() {}

    public int LineNumber { get; init; }
    public int CharNumber { get; init; }

    public override string ToString()
    {
        return $"({LineNumber}, {CharNumber})";
    }
}