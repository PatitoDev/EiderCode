using System.Collections.Generic;
using EiderCode.Engine.TokenGeneration;

namespace EiderCode.Engine.Models;

public enum DocumentLineStatus
{
    UnModified,
    Modified,
    Deleted,
    Added
}

public record DocumentLine
{
    public required int Index;
    public required CodeToken[] Tokens;
    public DocumentLineStatus Status = DocumentLineStatus.UnModified;
}

public record Document
{
    public required DocumentLine[] Lines;
}
