using EiderCode.Engine;
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

    public override void _Ready()
    {
        _codeRenderer = GetNode<CodeRenderer>("%CodeRenderer");
        _gutterRenderer = GetNode<GutterRenderer>("%GutterRenderer");


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
    }

    public void OpenFile(string filePath)
    {
        if (OpenFileCancellationTokenSource != null)
        {
            OpenFileCancellationTokenSource.Cancel();
        }
        OpenFileCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = OpenFileCancellationTokenSource.Token;

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
}
