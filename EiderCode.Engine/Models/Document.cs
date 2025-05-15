using System.Collections.Generic;
using EiderCode.Engine.TokenGeneration;

namespace EiderCode.Engine.Models;


public record DocumentLine
{
    public required IReadOnlyList<CodeToken> Tokens;
}

public record Document
{
    public required IReadOnlyList<DocumentLine> Lines;
}
