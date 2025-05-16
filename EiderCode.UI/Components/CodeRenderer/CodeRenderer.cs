using EiderCode.Engine;
using EiderCode.Engine.Models;
using Godot;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// TODO - when having multiple code renderes make sure to free canvas RIDs
public partial class CodeRenderer : Control
{
    public CodeEngine? _codeEngine;
    public TextServer? _textServer;
    public int? _fontSize;
    public Font? _font;
    public Vector2? _charSize;

    private Rid _canvasId;

    public CodeRenderer()
    {
        _canvasId = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_canvasId, GetCanvasItem());
    }

    public async Task OnLineParsedAsync(DocumentLine line, CancellationToken cancellation)
    {
        await Task.Run(() =>
        {
            if (!_charSize.HasValue) return;

            if (cancellation.IsCancellationRequested) return;
            RenderLine(line, cancellation);
            CallDeferred(CodeRenderer.MethodName.UpdateContainerSize);
        });
    }

    public void UpdateContainerSize()
    {
        if (!_charSize.HasValue) return;
        if (_codeEngine == null) return;
        CustomMinimumSize = new Vector2(0, _codeEngine.LineCount * _charSize.Value.Y);
    }

    public void OnFileOpen()
    {
        RenderingServer.CanvasItemClear(_canvasId);
    }

    public void ResetCanvas(Rid canvasIdToDelete)
    {
        RenderingServer.CanvasItemClear(canvasIdToDelete);
    }

    public void RenderDocument(CancellationToken cancellationToken)
    {
        if (_codeEngine == null) return;

        var tokens = _codeEngine.GetTokens();
        ResetCanvas(_canvasId);
        var tasks = tokens.Lines
        .Select(l => Task.Run(() => {
            if (cancellationToken.IsCancellationRequested) return;
            RenderLine(l, cancellationToken);
        }))
        .ToList();
    }

    private void RenderLine(DocumentLine line, CancellationToken cancellationToken)
    {
        if (_textServer == null) return;
        if (_font == null) return;
        if (!_fontSize.HasValue) return;
        if (!_charSize.HasValue) return;

        var position = new Vector2(0, (_charSize.Value.Y * (line.Index + 1)));

        var textId = _textServer.CreateShapedText(
          TextServer.Direction.Ltr,
          TextServer.Orientation.Horizontal
        );
            foreach (var token in line.Tokens)
            {
                _textServer.ShapedTextAddString(textId, token.Content, _font.GetRids(), _fontSize.Value);

                // TODO - cache colors
                // TODO - get fg default color from theme
                var tokenColor = token.Scopes.Length == 0 ?
                  Colors.White :
                  Color.FromString(token.Scopes[0].FgColor, Colors.White);

                if (cancellationToken.IsCancellationRequested) {
                  _textServer.FreeRid(textId);
                  return;
                }
                _textServer.ShapedTextDraw(textId, _canvasId, position, -1, -1, tokenColor);

                var textSize = _textServer.ShapedTextGetSize(textId);
                position = new Vector2(position.X + textSize.X, position.Y);

                _textServer.ShapedTextClear(textId);
            }
        _textServer.FreeRid(textId);
    }
}
