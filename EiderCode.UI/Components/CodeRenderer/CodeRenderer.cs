using EiderCode.Engine;
using EiderCode.Engine.Models;
using Godot;
using System.Threading;
using System.Threading.Tasks;

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
            RenderLine(line, new Vector2(0, (_charSize.Value.Y * (line.Index + 1))), cancellation);
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
        CallDeferred(CodeRenderer.MethodName.ResetCanvas, _canvasId);
        _canvasId = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_canvasId, GetCanvasItem());
    }

    public void ResetCanvas(Rid canvasIdToDelete)
    {
        RenderingServer.CanvasItemClear(canvasIdToDelete);
        RenderingServer.FreeRid(canvasIdToDelete);
    }

    /*
    private async Task RenderCodeFileAsync(Document document, CancellationToken cancellationToken)
    {
        RenderingServer.CanvasItemClear(_canvasId);

        var drawingCursor = new Vector2(0, _fontSize);

        var tasks = document
          .Lines
          .Select((line, index) => Task.Run(() =>
          {
              if (cancellationToken.IsCancellationRequested) return;
              RenderLine(line, new Vector2(0, (_charSize.Y * (index + 1))));
          }))
        .ToArray();

        await Task.WhenAll(tasks);
    }
*/

    private void RenderLine(DocumentLine line, Vector2 drawingCursor, CancellationToken cancellationToken)
    {
        if (_textServer == null) return;
        if (_font == null) return;
        if (!_fontSize.HasValue) return;

        var textId = _textServer.CreateShapedText(
          TextServer.Direction.Ltr,
          TextServer.Orientation.Horizontal
        );
            foreach (var token in line.Tokens)
            {
                _textServer.ShapedTextAddString(textId, token.Content, _font.GetRids(), _fontSize.Value);

                var tokenPosition = drawingCursor;

                // TODO - cache colors
                // TODO - get fg default color from theme
                var tokenColor = token.Scopes.Length == 0 ?
                  Colors.White :
                  Color.FromString(token.Scopes[0].FgColor, Colors.White);

                if (cancellationToken.IsCancellationRequested) {
                  _textServer.FreeRid(textId);
                  return;
                }
                _textServer.ShapedTextDraw(textId, _canvasId, tokenPosition, -1, -1, tokenColor);

                var textSize = _textServer.ShapedTextGetSize(textId);
                drawingCursor = new Vector2(drawingCursor.X + textSize.X, drawingCursor.Y);

                _textServer.ShapedTextClear(textId);
            }
        _textServer.FreeRid(textId);
    }
}
