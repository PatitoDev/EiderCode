using System.Collections.Generic;
using EiderCode.Engine.TokenGeneration;

namespace EiderCode.Engine.Models;

public record DocumentLine
{
    public required int Index;
    public required CodeToken[] Tokens;
}

public record Document
{
    public required DocumentLine[] Lines;
}
