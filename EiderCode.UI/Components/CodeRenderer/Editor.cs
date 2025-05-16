using EiderCode.Engine;
using EiderCode.Engine.Models;
using EiderCode.UI;
using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

public partial class Editor : MarginContainer
{
    private CodeEngine _codeEngine = new();
    private TextServer _textServer;

    private int _fontSize;
    private Font _font;
    private Vector2 _charSize;

    private CodeRenderer? _codeRenderer;
    private GutterRenderer? _gutterRenderer;
    private Cursor? _cursor;

    public Editor()
    {
        _fontSize = Theme.DefaultFontSize;
        _font = Theme.DefaultFont;
        _textServer = TextServerManager.GetPrimaryInterface();

        // assume font is monospace
        _charSize = _font.GetStringSize(
          "a",
          HorizontalAlignment.Left,
          -1,
          Theme.DefaultFontSize
        );
    }

    private CancellationTokenSource? OpenFileCancellationTokenSource;

    private CancellationTokenSource? ContentUpdateCancellationTokenSource;

    public override void _Ready()
    {
        _codeRenderer = GetNode<CodeRenderer>("%CodeRenderer");
        _gutterRenderer = GetNode<GutterRenderer>("%GutterRenderer");
        _cursor = GetNode<Cursor>("%Cursor");
        _cursor.BlockSize = _charSize;
        _cursor.Size = _charSize;

        _codeRenderer._fontSize = _fontSize;
        _codeRenderer._font = _font;
        _codeRenderer._charSize = _charSize;
        _codeRenderer._codeEngine = _codeEngine;
        _codeRenderer._textServer = _textServer;

        _gutterRenderer._fontSize = _fontSize;
        _gutterRenderer._font = _font;
        _gutterRenderer._charSize = _charSize;
        _gutterRenderer._codeEngine = _codeEngine;
        _gutterRenderer._textServer = _textServer;
        _gutterRenderer.initListeners();

        _codeEngine.OnCursorPositionChanged += (o, e) => {
          CallDeferred(Editor.MethodName.UpdateCursorPosition);
        };

        _codeEngine.OnModeChange += (o, e) => {
            if (_cursor == null) return;

            _cursor.SetCursorType(
                _codeEngine.CurrentMode == ViMode.Insert ?
                 CursorType.Line :
                 CursorType.Block
            );
        };

        _codeEngine.OnContentChanged += (o, e) => {
            if (ContentUpdateCancellationTokenSource != null) {
                ContentUpdateCancellationTokenSource.Cancel();
            }
            ContentUpdateCancellationTokenSource = new();
            var token = ContentUpdateCancellationTokenSource.Token;

            Task.Run(() => {
                _codeRenderer.RenderDocument(token);
            });
        };
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);

        if (
            @event is InputEventMouse &&
            DisplayServer.MouseGetMode() != DisplayServer.MouseMode.Visible
        )
        {
            DisplayServer.MouseSetMode(DisplayServer.MouseMode.Visible);
        }

        if (
            @event is InputEventKey &&
            ((InputEventKey)@event).IsPressed()
        )
        {
            DisplayServer.MouseSetMode(DisplayServer.MouseMode.Hidden);
            var inputEventKey = ((InputEventKey)@event);

            _codeEngine?.HandleKeyPress(new(){
                IsShiftPressed = inputEventKey.ShiftPressed,
                IsControlPressed = inputEventKey.CtrlPressed,
                KeyCode = inputEventKey.PhysicalKeycode,
                Unicode = inputEventKey.Unicode == 0 ? null : inputEventKey.Unicode
            });
        }
    }

    public void OpenFile(string filePath)
    {
        if (OpenFileCancellationTokenSource != null)
        {
            OpenFileCancellationTokenSource.Cancel();
        }
        OpenFileCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = OpenFileCancellationTokenSource.Token;

        UpdateCursorPosition();
        GrabFocus();

        _codeEngine.ClearOnLineParsedEvent();

        _codeEngine.OnLineParsed += async (o, ev) =>
        {
            if (_gutterRenderer == null) return;
            if (_codeRenderer == null) return;
            var line = ev.Line;
            if (cancellationToken.IsCancellationRequested) return;
            await _codeRenderer.OnLineParsedAsync(line, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
            await _gutterRenderer.OnLineParsedAsync(line, cancellationToken);
        };

        Task.Run(async () =>
        {
            if (cancellationToken.IsCancellationRequested) return;
            _codeRenderer?.OnFileOpen();
            _gutterRenderer?.OnFileOpen();
            if (cancellationToken.IsCancellationRequested) return;
            await _codeEngine.OpenFileAsync(filePath, cancellationToken);
        });
    }


    public void UpdateCursorPosition()
    {
        if (_codeEngine == null) return;

        var lineCount = _codeEngine.LineCount;

        var newPostion = ConvertToEditorPosition(_codeEngine.CursorPosition);
        _cursor?.MoveTo(newPostion);
            //_cursor?.SetChar(newPostion.Value.character);
    }

    public Vector2 ConvertToEditorPosition(EditorPosition position)
    {
        // top right
        var y = (position.LineNumber * _charSize.Y);
        var x = (position.CharNumber * _charSize.X);

        return new Vector2(x,y) + _codeRenderer!.GlobalPosition + new Vector2(0, 5);
    }
}
