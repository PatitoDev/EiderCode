using EiderCode.Engine;
using EiderCode.Engine.Models;
using Godot;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using EiderCode.UI;
using System.Diagnostics;
using System.Collections.Generic;

public record LineLabel
{
    public required List<TokenLabel> TokenLabels { get; init; }
}

public record TokenLabel
{
    public required Rid Rid { get; init; }
    // left baseline
    public required Vector2 Position { get; init; }
    public required Vector2 Size { get; init; }
    public required string Content { get; init; }
}

public partial class CodeRenderer : Control
{
    public CodeEngine? _codeEngine;
    public TextServer? _textServer;
    public int? _fontSize;
    public Font? _font;
    public Vector2? _charSize;
    private Cursor? _cursor;

    private Rid _canvasId;
    private List<LineLabel> _lineLabels = new();

    public CodeRenderer()
    {
        _canvasId = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_canvasId, GetCanvasItem());
    }

    public override void _Ready()
    {
        _cursor = GetNode<Cursor>("%Cursor");
    }

    public void SetupListeners()
    {
        if (_cursor == null) return;
        _cursor.BlockSize = _charSize!.Value;
        _cursor.Size = _charSize.Value;

        if (_codeEngine == null) return;
        _codeEngine.OnContentChanged += (o, e) =>
        {
            QueueRedraw();
        };

        _codeEngine.OnContentChangedAndCursorMoved += (o, e) =>
        {
            QueueRedraw();
            CallDeferred(CodeRenderer.MethodName.UpdateCursorPosition);
        };

        _codeEngine.OnLineParsed += (o, e) =>
        {
            CallDeferred(CodeRenderer.MethodName.QueueRedraw);
        };

        _codeEngine.OnFinishedParsing += (o, e) =>
        {
            CallDeferred(CodeRenderer.MethodName.QueueRedraw);
        };

        _codeEngine.OnModeChange += (o, e) =>
        {
            if (_cursor == null) return;

            _cursor.SetCursorType(
                _codeEngine.CurrentMode == ViMode.Insert ?
                 CursorType.Line :
                 CursorType.Block
            );
        };

        _codeEngine.OnCursorPositionChanged += (o, e) =>
        {
            CallDeferred(CodeRenderer.MethodName.UpdateCursorPosition);
        };
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _textServer?.FreeRid(_canvasId);
    }

    public override void _Draw()
    {
        base._Draw();
        if (_codeEngine == null) return;

        var timer = new Stopwatch();
        var documentLines = _codeEngine.GetTokens().Lines;
        timer.Start();
        RenderDocument();
        if (timer.ElapsedMilliseconds > 100)
        {
            GD.Print("Rendered document in: ", timer.ElapsedMilliseconds);
        }
        timer.Stop();
    }

    public void UpdateContainerSize()
    {
        if (!_charSize.HasValue) return;
        if (_codeEngine == null) return;
        CustomMinimumSize = new Vector2(0, _codeEngine.LineCount * _charSize.Value.Y);
    }

    public void OnFileOpen()
    {
        ResetCanvas(_canvasId);
    }

    public void ResetCanvas(Rid canvasIdToDelete)
    {
        RenderingServer.CanvasItemClear(canvasIdToDelete);
    }

    public void RenderDocument()
    {
        if (_codeEngine == null) return;

        var tokens = _codeEngine.GetTokens();
        ResetCanvas(_canvasId);
        foreach (var l in tokens.Lines)
        {
            RenderLine(l, CancellationToken.None);
        }
    }

    private void RenderLine(DocumentLine line, CancellationToken cancellationToken)
    {
        if (_textServer == null) return;
        if (_font == null) return;
        if (!_fontSize.HasValue) return;
        if (!_charSize.HasValue) return;

        var position = new Vector2(0, (_charSize.Value.Y * (line.Index + 1)));

        var lineLabel = _lineLabels.ElementAtOrDefault(line.Index);
        if (lineLabel == null)
        {
            lineLabel = new LineLabel()
            {
                TokenLabels = new()
            };
            _lineLabels.Insert(line.Index, lineLabel);
        }

        var tokenIndex = 0;
        foreach (var token in line.Tokens)
        {
            var existingTokenLabel = lineLabel.TokenLabels.ElementAtOrDefault(tokenIndex);
            Rid? textId = null;

            if (existingTokenLabel != null)
            {
                textId = existingTokenLabel.Rid;
                // make sure its clean
                _textServer.ShapedTextClear(textId.Value);
            }
            else
            {
                textId = _textServer.CreateShapedText(
                  TextServer.Direction.Ltr,
                  TextServer.Orientation.Horizontal
                );
            }

            // could check if we need to redraw or not

            _textServer.ShapedTextAddString(
                textId.Value,
                token.Content,
                _font.GetRids(),
                _fontSize.Value
            );

            _textServer.ShapedTextDraw(
                textId.Value,
                _canvasId,
                position,
                -1,
                -1,
                token.FgColor
            );

            var textSize = _textServer.ShapedTextGetSize(textId.Value);
            var tokenLabel = new TokenLabel()
            {
                Content = token.Content,
                Position = position,
                Rid = textId.Value,
                Size = textSize
            };


            if (existingTokenLabel == null)
            {
                lineLabel.TokenLabels.Insert(tokenIndex, tokenLabel);
            }
            else
            {
                lineLabel.TokenLabels[tokenIndex] = tokenLabel;
            }

            // update position for next token;
            position = new Vector2(position.X + textSize.X, position.Y);
            tokenIndex += 1;
        }

        var tokensLeftToClear = lineLabel.TokenLabels.Count;

        for (var index = tokenIndex; index < tokensLeftToClear; index++)
        {
            var last = lineLabel.TokenLabels.Last();
            _textServer.FreeRid(last.Rid);

            lineLabel.TokenLabels.RemoveAt(lineLabel.TokenLabels.Count - 1);
        }
    }

    public void UpdateCursorPosition()
    {
        if (_codeEngine == null) return;

        var lineCount = _codeEngine.LineCount;

        var newCaretBounds = GetCharPosition(_codeEngine.CursorPosition);
        if (newCaretBounds == null) return;
        //_cursor?.MoveTo(newPosition);
        _cursor?.UpdateCursorSizeAndBounds(newCaretBounds.Value.pos, newCaretBounds.Value.size);
        //_cursor?.SetChar(newPostion.Value.character);
    }

    // only call after render has finished
    public (Vector2 pos, Vector2 size)? GetCharPosition(EditorPosition position)
    {
        if (_textServer == null) return null;

        var charSize = _charSize!.Value;
        // top right
        var y = (position.LineNumber * charSize.Y);

        var line = _lineLabels.ElementAtOrDefault(position.LineNumber);
        if (line == null) return null; // handle null
        var targetCharPosition = position.CharNumber;
        var charCount = 0;

        if (line.TokenLabels.Sum(t => t.Content.Length) == position.CharNumber)
        {
            // we are at end of line
            var last = line.TokenLabels.LastOrDefault();
            if (last == null)
            {
                GD.Print(last);
                var startPosition = new Vector2(0, y);
                return (startPosition, _charSize.Value);
            }
            return (last.Position + new Vector2(last.Size.X, 0), _charSize.Value);
        }

        foreach (var token in line.TokenLabels)
        {
            if (targetCharPosition >= charCount &&
                targetCharPosition < charCount + token.Content.Length
                )
            {
                // found token with char inside
                var relativeGraphemePosition = targetCharPosition - charCount;
                var bounds = _textServer.ShapedTextGetGraphemeBounds(token.Rid, relativeGraphemePosition);
                var carets = _textServer.ShapedTextGetCarets(token.Rid, relativeGraphemePosition);
                var caretLeadingRect = (carets["leading_rect"]).AsRect2();
                var caretTrailingRect = (carets["trailing_rect"]).AsRect2();

                var foundPosition = token.Position + new Vector2(caretLeadingRect.Position.X, 0);

                // don't use caret y size because I don't understand it and it makes no sense
                var size = new Vector2(caretTrailingRect.Size.X, _charSize.Value.Y);

                return (foundPosition, size);
            }
            charCount += token.Content.Length;
        }

        GD.Print(" no token found");
        return null;
    }
}
