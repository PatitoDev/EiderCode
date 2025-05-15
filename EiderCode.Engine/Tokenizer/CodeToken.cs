using System.Collections.Generic;

namespace EiderCode.Engine.TokenGeneration;

public record CodeToken
{
    public required string Content { get; init; }
    public required Scope[] Scopes { get; init; }
}