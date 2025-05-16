using EiderCode.Engine;
using EiderCode.Engine.Models;
using Godot;
using System.Threading;
using System.Threading.Tasks;
using System;

public partial class GutterRenderer : Control
{
    public CodeEngine? _codeEngine;
    public TextServer? _textServer;
    public int? _fontSize;
    public Font? _font;
    public Vector2? _charSize;

    private Rid _canvasId;

    private int _maxLineCountLength = 4;

    public GutterRenderer()
    {
        _canvasId = RenderingServer.CanvasItemCreate();
        RenderingServer.CanvasItemSetParent(_canvasId, GetCanvasItem());
    }

    public void initListeners()
    {
      if (_codeEngine == null) return;
      _codeEngine.OnFinishedParsing += (o, ev) => {
        if (_codeEngine.LineCount.ToString().Length > _maxLineCountLength){
          CallDeferred(GutterRenderer.MethodName.RenderGutter);
        }
        CallDeferred(GutterRenderer.MethodName.UpdateContainerSize);
      };

      _codeEngine.OnCursorPositionChanged += (o, ev) => {
          CallDeferred(GutterRenderer.MethodName.RenderGutter);
      };
    }

    public async Task OnLineParsedAsync(DocumentLine line, CancellationToken cancellation)
    {
        await Task.Run(() =>
        {
            if (!_charSize.HasValue) return;

            if (cancellation.IsCancellationRequested) return;
            var lineWidth = line.Index.ToString().Length;
            if (lineWidth > _maxLineCountLength) {
              _maxLineCountLength = lineWidth;
            }
            RenderLine(line, _canvasId, cancellation);
        });
    }

    public void UpdateContainerSize()
    {
        if (!_charSize.HasValue) return;
        if (_codeEngine == null) return;
        var maxLineCount = Math.Max(
          _codeEngine.Lines.Count.ToString().Length,
          _maxLineCountLength
        );

        var width = (maxLineCount + 2) * _charSize.Value.X;
        var windowHeight = DisplayServer.WindowGetSize().X;
        var extraBottomPadding = ((int)(windowHeight / _charSize.Value.Y));
        var height = (_codeEngine.LineCount + extraBottomPadding) * _charSize.Value.Y;

        CustomMinimumSize = new Vector2(width, height);
    }

    public void OnFileOpen()
    {
        _maxLineCountLength = 4;
        RenderingServer.CanvasItemClear(_canvasId);

        CallDeferred(CodeRenderer.MethodName.UpdateContainerSize);
    }

    private void RenderGutter()
    {
      if (_codeEngine == null) return;

      RenderingServer.CanvasItemClear(_canvasId);

      foreach (var line in _codeEngine.DocumentLines.ToArray()){
        RenderLine(line, _canvasId, CancellationToken.None);
      }
    }

    private void RenderLine(DocumentLine line, Rid canvasId, CancellationToken cancellationToken)
    {
        if (_charSize == null) return;
        if (_textServer == null) return;
        if (_font == null) return;
        if (!_fontSize.HasValue) return;

        var textId = _textServer.CreateShapedText(
          TextServer.Direction.Ltr,
          TextServer.Orientation.Horizontal
        );

        /*
        RenderingServer.CanvasItemAddLine(_canvasId,
          new Vector2(0, _charSize.Value.Y * (line.Index)),
          new Vector2(0, _charSize.Value.Y * (line.Index + 1)),
          Colors.Green,
          6
        );
        */

        _textServer.ShapedTextAddString(textId, line.Index.ToString(), _font.GetRids(), _fontSize.Value);
        if (cancellationToken.IsCancellationRequested)
        {
          _textServer.FreeRid(textId);
          return;
        }

        var color = _codeEngine!.GetGuiColor(GuiThemeKeys.EditorFg);
        var isOnLine = _codeEngine!.CursorPosition.LineNumber == line.Index;
        color.A = isOnLine ? 1f : 0.6f;

        var offsetXForRightAlign = (_maxLineCountLength - (line.Index.ToString().Length)) * _charSize.Value.X;

        var position = new Vector2(
          offsetXForRightAlign,
          (_charSize.Value.Y * (line.Index + 1)
        ));

        if (!canvasId.IsValid) return;
        _textServer.ShapedTextDraw(textId, canvasId, position, -1, -1, color);
        var textSize = _textServer.ShapedTextGetSize(textId);

        _textServer.ShapedTextClear(textId);
        _textServer.FreeRid(textId);
    }
}
