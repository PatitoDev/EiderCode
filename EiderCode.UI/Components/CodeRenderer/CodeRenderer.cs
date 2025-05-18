using EiderCode.Engine;
using EiderCode.Engine.Models;
using Godot;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EiderCode.UI;
using System.Diagnostics;

public partial class CodeRenderer : Control
{
    public CodeEngine? _codeEngine;
    public TextServer? _textServer;
    public int? _fontSize;
    public Font? _font;
    public Vector2? _charSize;
    private Cursor? _cursor;

    private Rid _canvasId;

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
            //RenderDocument(CancellationToken.None);
            GD.Print("content chagned");
            QueueRedraw();
        };

        _codeEngine.OnContentChangedAndCursorMoved += (o, e) =>
        {
            GD.Print("content chagned and cursor");
            //RenderDocument(CancellationToken.None);
            QueueRedraw();
            CallDeferred(CodeRenderer.MethodName.UpdateCursorPosition);
        };

        _codeEngine.OnLineParsed += (o,e) => {
            CallDeferred(CodeRenderer.MethodName.QueueRedraw);
        };

        _codeEngine.OnFinishedParsing += (o,e) =>
        {
            //CallDeferred(CodeRenderer.MethodName.QueueRedraw);
        };

        _codeEngine.OnModeChange += (o, e) => {
            if (_cursor == null) return;

            _cursor.SetCursorType(
                _codeEngine.CurrentMode == ViMode.Insert ?
                 CursorType.Line :
                 CursorType.Block
            );
        };

        _codeEngine.OnCursorPositionChanged += (o, e) => {
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
        GD.Print("Rendered document in: ", timer.ElapsedMilliseconds);
        timer.Stop();
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
        GD.Print("Reset canvas");
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
        foreach (var l in tokens.Lines) {
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

            if (cancellationToken.IsCancellationRequested)
            {
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

    public void UpdateCursorPosition()
    {
        if (_codeEngine == null) return;

        var lineCount = _codeEngine.LineCount;

        var newPosition = GetCharPosition(_codeEngine.CursorPosition);
        _cursor?.MoveTo(newPosition);
            //_cursor?.SetChar(newPostion.Value.character);
    }

    public Vector2 GetCharPosition(EditorPosition position)
    {
        var charSize = _charSize!.Value;
        // top right
        var y = (position.LineNumber * charSize.Y);
        var x = (position.CharNumber * (charSize.X));

        return new Vector2(x, y) + GlobalPosition + new Vector2(0, 5);
    }
}
